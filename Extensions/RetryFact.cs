using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Extensions
{
    /// <summary>
    ///     Works just like [Fact] except that failures are retried (by default, 3
    ///     times).
    /// </summary>
    [XunitTestCaseDiscoverer("PLX.Test.UI.Tests.Utility.RetryFactDiscoverer", "PLX.Test.UI.Tests")]
    public class RetryFactAttribute : FactAttribute
    {
        #region Properties

        /// <summary>
        ///     Number of retries allowed for a failed test. If unset (or set less
        ///     than 1), will default to 3 attempts.
        /// </summary>
        public int MaxRetries { get; set; }

        #endregion
    }

    [Serializable]
    public class RetryTestCase : XunitTestCase
    {
        #region Constants

        private static readonly ILogger _Log = Log.ForContext("SourceContext", nameof(RetryTestCase));

        #endregion

        #region Fields

        private int _MaxRetries;

        #endregion

        #region Properties

        public RunSummary Summary { get; private set; }

        #endregion

        #region Constructors

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Called by the de-serializer", true)]
        public RetryTestCase()
        {
        }

        public RetryTestCase(
            IMessageSink diagnosticMessageSink,
            TestMethodDisplay testMethodDisplay,
            ITestMethod testMethod,
            int maxRetries)
            : base(diagnosticMessageSink, testMethodDisplay, testMethod)
        {
            _MaxRetries = maxRetries;
        }

        #endregion

        #region Methods

        // This method is called by the xUnit test framework classes to run the test case. We will do the
        // loop here, forwarding on to the implementation in XunitTestCase to do the heavy lifting. We will
        // continue to re-run the test until the aggregator has an error (meaning that some internal error
        // condition happened), or the test runs without failure, or we've hit the maximum number of tries.
        public override async Task<RunSummary> RunAsync(
            IMessageSink diagnosticMessageSink,
            IMessageBus messageBus,
            object[] constructorArguments,
            ExceptionAggregator aggregator,
            CancellationTokenSource cancellationTokenSource)
        {
            var runCount = 0;

            while (true)
            {
                // This is really the only tricky bit: we need to capture and delay messages (since those will
                // contain run status) until we know we've decided to accept the final result;
                var delayedMessageBus = new DelayedMessageBus(messageBus);

                var summary = await base.RunAsync(diagnosticMessageSink, delayedMessageBus, constructorArguments,
                    aggregator, cancellationTokenSource).ConfigureAwait(false);
                if (aggregator.HasExceptions || summary.Failed == 0 || ++runCount >= _MaxRetries)
                {
                    Summary = summary;
                    delayedMessageBus.Dispose(); // Sends all the delayed messages
                    return summary;
                }

                _Log.Debug("Execution of '{0}' failed (attempt #{1}), retrying...", DisplayName, runCount);
                diagnosticMessageSink.OnMessage(
                    new DiagnosticMessage("Execution of '{0}' failed (attempt #{1}), retrying...", DisplayName,
                        runCount));
            }
        }

        public override void Serialize(IXunitSerializationInfo data)
        {
            base.Serialize(data);

            data.AddValue("MaxRetries", _MaxRetries);
            data.AddValue("Summary", Summary);
        }

        public override void Deserialize(IXunitSerializationInfo data)
        {
            base.Deserialize(data);

            _MaxRetries = data.GetValue<int>("MaxRetries");
            Summary = data.GetValue<RunSummary>("Summary");
        }

        #endregion
    }

    public class RetryFactDiscoverer : IXunitTestCaseDiscoverer
    {
        #region Fields

        private readonly IMessageSink _DiagnosticMessageSink;

        #endregion

        #region Constructors

        public RetryFactDiscoverer(IMessageSink diagnosticMessageSink)
        {
            _DiagnosticMessageSink = diagnosticMessageSink;
        }

        #endregion

        #region Methods

        public IEnumerable<IXunitTestCase> Discover(ITestFrameworkDiscoveryOptions discoveryOptions,
            ITestMethod testMethod, IAttributeInfo factAttribute)
        {
            var maxRetries = factAttribute.GetNamedArgument<int>("MaxRetries");
            if (maxRetries < 1)
                maxRetries = 3;

            yield return new RetryTestCase(_DiagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(),
                testMethod, maxRetries);
        }

        #endregion
    }

    public class DelayedMessageBus : IMessageBus
    {
        #region Constructors

        public DelayedMessageBus(IMessageBus innerBus)
        {
            _InnerBus = innerBus;
        }

        #endregion

        #region Fields

        private readonly IMessageBus _InnerBus;
        private readonly List<IMessageSinkMessage> _Messages = new List<IMessageSinkMessage>();

        #endregion

        #region Methods

        public bool QueueMessage(IMessageSinkMessage message)
        {
            lock (_Messages)
            {
                _Messages.Add(message);
            }

            // No way to ask the inner bus if they want to cancel without sending them the message, so
            // we just go ahead and continue always.
            return true;
        }

        public void Dispose()
        {
            foreach (var message in _Messages)
                _InnerBus.QueueMessage(message);
        }

        #endregion
    }
}