using System.Text.Json;
using System.Threading.Tasks.Dataflow;
using AiAgentService.Web.Configurations;
using AiAgentService.Web.Pipeline.Data;
using AiAgentService.Web.Pipeline.Filters;
using AiAgentService.Web.Pipeline.Handlers.Kafka;
using AiAgentService.Web.Pipeline.Handlers.Summarization;
using Confluent.Kafka;
using LinkTracker.Scrapper.Contracts.Dtos.Notifications;
using Microsoft.Extensions.Options;

namespace AiAgentService.Web.Pipeline;

internal class MessageProcessingPipeline
{
    private readonly TransformBlock<MessageWrap, MessageWrap> _filterBlock;
    private readonly TransformBlock<MessageWrap, MessageWrap?> _summarizeBlock;
    private readonly ActionBlock<MessageWrap> _messagesAckBlock;
    private readonly ActionBlock<MessageWrap> _finalBlock;
    private readonly ILogger<MessageProcessingPipeline> _logger;

    public MessageProcessingPipeline(AggregateFilter aggregateFilter, SendToKafkaHandler kafkaSender, IServiceScopeFactory scopeFactory, ILogger<MessageProcessingPipeline> logger, IOptions<FiltersConfiguration> config)
    {
        _logger = logger;
        _filterBlock = new TransformBlock<MessageWrap, MessageWrap>(aggregateFilter.Handle);

        _summarizeBlock = DataflowExtensions.CreateScopedTransformBlock<MessageWrap, MessageWrap, SummarizationHandler>
            (scopeFactory, new ExecutionDataflowBlockOptions()
            {
                MaxDegreeOfParallelism = config.Value.Summarization.NumberOfParallelRequests,
                BoundedCapacity = config.Value.Summarization.BoundedCapacity
            }, logger
        );
        _messagesAckBlock = new ActionBlock<MessageWrap>(m =>
        {
            m.Consumer.Commit([m.Offset]);
            _logger.LogInformation("Message {offset} acknowledged, IsFiltered={f}", m.Offset.Offset, m.IsFiltered);
        });
        _finalBlock = new ActionBlock<MessageWrap>(async m =>
        {
            try
            {
                var sent = await kafkaSender.Handle(m.Message);
                if (sent)
                {
                    m.Consumer.Commit([m.Offset]);
                    _logger.LogInformation("Message {offset} acknowledged after storing offset, IsFiltered={f}", m.Offset.Offset, m.IsFiltered);
                }
                else
                {
                    _logger.LogWarning("Failed to send message to kafka with offset={offset}", m.Offset);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to commit/push notification to kafka");
            }
        });
        var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };

        _filterBlock.LinkTo(_messagesAckBlock, m => m.IsFiltered);
        _filterBlock.LinkTo(_summarizeBlock, m => !m.IsFiltered);

        _summarizeBlock.LinkTo(_finalBlock, linkOptions, m => m != null);

        _filterBlock.Completion.ContinueWith(_ =>
        {
            _messagesAckBlock.Complete();
            _finalBlock.Complete();
        });
    }

    public async Task PushMessage(ConsumeResult<Null, string> message, IConsumer<Null, string> consumer)
    {
        try
        {
            var dto = JsonSerializer.Deserialize<UpdateMessage>(message.Message.Value);
            if (dto == null)
            {
                _logger.LogError("Cannot deserialize dto! Received message: {msg}", message);
                return;
            }

            var wrap = new MessageWrap(dto, message.TopicPartitionOffset, consumer);
            await _filterBlock.SendAsync(wrap);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to push message to pipeline");
        }
    }

    public async Task Complete()
    {
        _filterBlock.Complete();
        await Task.WhenAll(_finalBlock.Completion, _messagesAckBlock.Completion);
    }
}