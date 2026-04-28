using System.Threading.Tasks.Dataflow;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Notifications;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Data;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Results;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Steps;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow;

public class StackOverflowPipeline
{
    private readonly ITargetBlock<List<QuestionData>> _entryPoint;

    public ISourceBlock<NotificationMessage> OutputBlock { get; }

    public StackOverflowPipeline(
        FetchQuestionsStep fetchQuestions,
        ClearQuestionsStep clearQuestions,
        FetchAnswersStep fetchAnswers,
        ClearAnswersStep clearAnswers,
        ProcessAnswersStep processAnswers,
        FetchCommentsStep fetchComments,
        ProcessCommentsStep processComments,
        FetchQuestionCommentsStep fetchQuestionComments,
        ProcessQuestionCommentsStep processQuestionComments,
        IOptions<StackOverflowPipelineOptions> options,
        ILogger<StackOverflowPipeline> logger)
    {
        var dataflowOptions = new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = options.Value.MaxDegreeOfParallelism,
            BoundedCapacity = DataflowBlockOptions.Unbounded,
            EnsureOrdered = false
        };

        var outputBuffer = new BufferBlock<NotificationMessage>(new DataflowBlockOptions
        {
            BoundedCapacity = DataflowBlockOptions.Unbounded,
            EnsureOrdered = false
        });

        var fetchQuestionsBlock = new TransformBlock<List<QuestionData>, FetchQuestionsResult>(
            async batch => await fetchQuestions.ProcessAsync(batch), dataflowOptions);

        var clearQuestionsBlock = new TransformBlock<FetchQuestionsResult, IReadOnlyList<QuestionData>>(
            async result => await clearQuestions.ProcessAsync(result), dataflowOptions);

        var broadcastQuestionsBlock = new BroadcastBlock<IReadOnlyList<QuestionData>>(
            questions => questions, new DataflowBlockOptions { BoundedCapacity = DataflowBlockOptions.Unbounded });

        var fetchQuestionCommentsBlock = new TransformBlock<IReadOnlyList<QuestionData>, FetchQuestionCommentsResult>(
            async questions => await fetchQuestionComments.ProcessAsync(questions), dataflowOptions);

        var processQuestionCommentsBlock = new ActionBlock<FetchQuestionCommentsResult>(
            async result => await processQuestionComments.ProcessAsync(result), dataflowOptions);

        var fetchAnswersBlock = new TransformManyBlock<IReadOnlyList<QuestionData>, FetchAnswersResult>(
            async batch => await fetchAnswers.ProcessAsync(batch), dataflowOptions);

        var clearAnswersBlock = new TransformBlock<FetchAnswersResult, IReadOnlyList<AnswerData>>(
            async result => await clearAnswers.ProcessAsync(result), dataflowOptions);

        var broadcastAnswersBlock = new BroadcastBlock<IReadOnlyList<AnswerData>>(
            answers => answers, new DataflowBlockOptions { BoundedCapacity = DataflowBlockOptions.Unbounded });

        var processAnswersBlock = new ActionBlock<IReadOnlyList<AnswerData>>(
            async answers => await processAnswers.ProcessAsync(answers), dataflowOptions);

        var fetchCommentsBlock = new TransformBlock<IReadOnlyList<AnswerData>, FetchCommentsResult>(
            async answers => await fetchComments.ProcessAsync(answers), dataflowOptions);

        var processCommentsBlock = new ActionBlock<FetchCommentsResult>(
            async result => await processComments.ProcessAsync(result), dataflowOptions);

        var linkOptions = new DataflowLinkOptions { PropagateCompletion = false };

        // flow: FetchQ -> ClearQ -> BroadcastQ
        fetchQuestionsBlock.LinkTo(clearQuestionsBlock, linkOptions);
        clearQuestionsBlock.LinkTo(broadcastQuestionsBlock, linkOptions);

        // path 1: BroadcastQ -> FetchQComments -> ProcessQComments
        broadcastQuestionsBlock.LinkTo(fetchQuestionCommentsBlock, linkOptions);
        fetchQuestionCommentsBlock.LinkTo(processQuestionCommentsBlock, linkOptions);

        // path 2: BroadcastQ -> FetchA -> ClearA -> BroadcastA
        broadcastQuestionsBlock.LinkTo(fetchAnswersBlock, linkOptions);
        fetchAnswersBlock.LinkTo(clearAnswersBlock, linkOptions);
        clearAnswersBlock.LinkTo(broadcastAnswersBlock, linkOptions);

        // path 2.1: BroadcastA -> ProcessA
        broadcastAnswersBlock.LinkTo(processAnswersBlock, linkOptions);

        // path 2.2: BroadcastA -> FetchAComments -> ProcessAComments
        broadcastAnswersBlock.LinkTo(fetchCommentsBlock, linkOptions);
        fetchCommentsBlock.LinkTo(processCommentsBlock, linkOptions);

        void LogFault(Task t)
        {
            if (t is { IsFaulted: true, Exception: not null })
            {
                logger.LogCritical(t.Exception, "StackOverflow pipeline block faulted");
            }
        }

        fetchQuestionsBlock.Completion.ContinueWith(LogFault);
        clearQuestionsBlock.Completion.ContinueWith(LogFault);
        fetchAnswersBlock.Completion.ContinueWith(LogFault);
        clearAnswersBlock.Completion.ContinueWith(LogFault);
        fetchQuestionCommentsBlock.Completion.ContinueWith(LogFault);
        processQuestionCommentsBlock.Completion.ContinueWith(LogFault);
        processAnswersBlock.Completion.ContinueWith(LogFault);
        fetchCommentsBlock.Completion.ContinueWith(LogFault);
        processCommentsBlock.Completion.ContinueWith(LogFault);

        _entryPoint = fetchQuestionsBlock;
        OutputBlock = outputBuffer;
    }

    public async Task PostBatch(List<QuestionData> batch)
    {
        await _entryPoint.SendAsync(batch);
    }
}
