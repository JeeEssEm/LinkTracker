using System.Threading.Tasks.Dataflow;
using AiAgentService.Web.Configurations;
using AiAgentService.Web.Infrastructure;
using AiAgentService.Web.Pipeline.Data;
using AiAgentService.Web.Pipeline.Filters;
using AiAgentService.Web.Pipeline.Handlers.Grouping;
using AiAgentService.Web.Pipeline.Handlers.Kafka;
using AiAgentService.Web.Pipeline.Handlers.Prioritizing;
using AiAgentService.Web.Pipeline.Handlers.Summarization;
using LinkTracker.Bot.Contracts.Rest;
using Microsoft.Extensions.Options;

namespace AiAgentService.Web.Pipeline;

public class MessageProcessingPipeline
{
    private readonly TransformBlock<MessageData, MessageData> _filterBlock;
    private readonly TransformManyBlock<MessageData[], GroupedMessages> _grouperBlock;
    private readonly TransformBlock<MessageData, MessageData> _summarizeBlock;
    private readonly TransformBlock<MessageData, MessageData> _prioritizeBlock;
    private readonly ActionBlock<MessageData> _messagesAckBlock;
    private readonly ActionBlock<GroupedMessages> _finalBlock;
    private readonly BatchBlock<MessageData> _batchBlock;
    private readonly IConsumerUpdatesManager _consumerManager;
    private readonly ILogger<MessageProcessingPipeline> _logger;
    private readonly Timer _batchTimer;

    public MessageProcessingPipeline(
        AggregateFilter aggregateFilter,
        ISendToKafkaHandler kafkaSender,
        IServiceScopeFactory scopeFactory,
        IConsumerUpdatesManager consumerManager,
        ILogger<MessageProcessingPipeline> logger,
        IOptions<FiltersConfiguration> config,
        IOptions<PrioritizationConfiguration> priorityConfig,
        IOptions<GroupingConfiguration> groupingConfig
        )
    {
        _consumerManager = consumerManager;
        _logger = logger;

        _filterBlock = new TransformBlock<MessageData, MessageData>(aggregateFilter.Handle);

        _summarizeBlock = DataflowExtensions.CreateScopedTransformBlock<MessageData, MessageData, SummarizationHandler>
            (scopeFactory, new ExecutionDataflowBlockOptions()
            {
                MaxDegreeOfParallelism = config.Value.Summarization.NumberOfParallelRequests,
                BoundedCapacity = config.Value.Summarization.BoundedCapacity
            }, logger
        );
        _prioritizeBlock = DataflowExtensions.CreateScopedTransformBlock<MessageData, MessageData, PrioritizingHandler>(
            scopeFactory, new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = priorityConfig.Value.DegreeOfParallelism,
                BoundedCapacity = priorityConfig.Value.BoundedCapacity
            }, logger
        );

        _batchBlock = new BatchBlock<MessageData>(groupingConfig.Value.MaxGroupBatchSize, new GroupingDataflowBlockOptions()
        {
            EnsureOrdered = true
        });
        _batchTimer = new Timer(_ =>
        {
            if (_batchBlock.BatchSize > 0)
            {
                _batchBlock.TriggerBatch();
            }
        }, null, TimeSpan.FromMilliseconds(groupingConfig.Value.WindowMs), TimeSpan.FromMilliseconds(groupingConfig.Value.WindowMs));

        _grouperBlock = new TransformManyBlock<MessageData[], GroupedMessages>(GroupingHandler.Group, new ExecutionDataflowBlockOptions() { MaxDegreeOfParallelism = 1 });
        _messagesAckBlock = new ActionBlock<MessageData>(m =>
        {
            _consumerManager.Commit([m.Offset], m.Message.TgChatIds);
            _logger.LogInformation("Message {offset} acknowledged, IsFiltered={f}", m.Offset.Offset, m.IsFiltered);
        });
        _finalBlock = new ActionBlock<GroupedMessages>(async m =>
        {
            try
            {
                var sent = await kafkaSender.Handle(new LinkUpdateRequest(Guid.NewGuid(), m.Description, m.ChatIds, m.Priority));
                if (sent)
                {
                    _consumerManager.Commit(m.MessageIds, m.ChatIds);
                }
                else
                {
                    _logger.LogWarning("Failed to send message to kafka with source offsets={offset}", m.MessageIds);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to commit/push notification to kafka");
            }
        });
        var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };

        _filterBlock.LinkTo(_messagesAckBlock, m => m.IsFiltered);
        _filterBlock.LinkTo(_prioritizeBlock, m => !m.IsFiltered); // TODO: а может лучше после группировки, чтоб лишний раз не ставить приоритет?
        _prioritizeBlock.LinkTo(_summarizeBlock, linkOptions);

        _summarizeBlock.LinkTo(_batchBlock, linkOptions);
        _batchBlock.LinkTo(_grouperBlock, linkOptions);
        _grouperBlock.LinkTo(_finalBlock, linkOptions);

        _filterBlock.Completion.ContinueWith(_ =>
        {
            _prioritizeBlock.Complete();
            _messagesAckBlock.Complete();
            _batchTimer.Dispose();
        });
    }

    public async Task PushMessage(MessageData message)
    {
        await _filterBlock.SendAsync(message);
    }

    public async Task Complete()
    {
        _filterBlock.Complete();
        await Task.WhenAll(_finalBlock.Completion, _messagesAckBlock.Completion);
    }
}