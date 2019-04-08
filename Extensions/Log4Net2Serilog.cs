using System;
using System.Linq.Expressions;
using System.Reflection;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Repository.Hierarchy;
using log4net.Util;
using Serilog;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;
using ILogger = Serilog.ILogger;

namespace Extensions
{
    public static class Log4Net2Serilog
    {
        /// <summary>
        ///     Configures log4net to log to Serilog.
        /// </summary>
        /// <param name="logger">The serilog logger (if left null Log.Logger will be used).</param>
        public static void Configure(ILogger logger = null)
        {
            var serilogAppender = new SerilogAppender(logger);
            serilogAppender.ActivateOptions();
            var loggerRepository = (Hierarchy) LogManager.GetRepository(Assembly.GetCallingAssembly());
            if (loggerRepository.Root.GetAppender(serilogAppender.Name) == null)
                loggerRepository.Root.AddAppender(serilogAppender);
            loggerRepository.Configured = true;
        }

        private class SerilogAppender : AppenderSkeleton
        {
            private static readonly Func<SystemStringFormat, string> FormatGetter;
            private static readonly Func<SystemStringFormat, object[]> ArgumentsGetter;
            private readonly ILogger _Logger;

            static SerilogAppender()
            {
                FormatGetter = GetFieldAccessor<SystemStringFormat, string>("m_format");
                ArgumentsGetter = GetFieldAccessor<SystemStringFormat, object[]>("m_args");
            }

            public SerilogAppender(ILogger logger = null)
            {
                _Logger = logger;
            }


            protected override void Append(LoggingEvent loggingEvent)
            {
                var source = loggingEvent.LoggerName;
                var serilogLevel = ConvertLevel(loggingEvent.Level);
                string template;
                object[] parameters = null;

                if (loggingEvent.MessageObject is SystemStringFormat systemStringFormat)
                {
                    template = FormatGetter(systemStringFormat);
                    parameters = ArgumentsGetter(systemStringFormat);
                }
                else
                    template = loggingEvent.MessageObject?.ToString();

                var logger = (_Logger ?? Log.Logger).ForContext(Constants.SourceContextPropertyName, source);
                logger.Write(serilogLevel, loggingEvent.ExceptionObject, template, parameters);
            }

            private static LogEventLevel ConvertLevel(Level log4NetLevel)
            {
                if (log4NetLevel == Level.Verbose) return LogEventLevel.Verbose;
                if (log4NetLevel == Level.Debug) return LogEventLevel.Debug;
                if (log4NetLevel == Level.Info) return LogEventLevel.Information;
                if (log4NetLevel == Level.Warn) return LogEventLevel.Warning;
                if (log4NetLevel == Level.Error) return LogEventLevel.Error;
                if (log4NetLevel == Level.Fatal) return LogEventLevel.Fatal;
                SelfLog.WriteLine("Unexpected log4net logging level ({0}) logging as Information",
                    log4NetLevel.DisplayName);
                return LogEventLevel.Information;
            }

            //taken from http://rogeralsing.com/2008/02/26/linq-expressions-access-private-fields/
            public static Func<T, TField> GetFieldAccessor<T, TField>(string fieldName)
            {
                var param = Expression.Parameter(typeof(T), "arg");
                var member = Expression.Field(param, fieldName);
                var lambda = Expression.Lambda(typeof(Func<T, TField>), member, param);
                var compiled = (Func<T, TField>) lambda.Compile();
                return compiled;
            }
        }
    }
}