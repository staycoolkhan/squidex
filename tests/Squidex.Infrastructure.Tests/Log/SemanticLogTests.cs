﻿// ==========================================================================
//  SemanticLogTests.cs
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex Group
//  All rights reserved.
// ==========================================================================

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Moq;
using Squidex.Infrastructure.Log.Adapter;
using Xunit;

namespace Squidex.Infrastructure.Log
{
    public class SemanticLogTests
    {
        private readonly List<ILogAppender> appenders = new List<ILogAppender>();
        private readonly List<ILogChannel> channels = new List<ILogChannel>();
        private readonly Lazy<SemanticLog> log;
        private readonly Mock<ILogChannel> channel = new Mock<ILogChannel>();
        private string output;

        public SemanticLog Log
        {
            get { return log.Value; }
        }

        public SemanticLogTests()
        {
            channels.Add(channel.Object);

            channel.Setup(x => x.Log(It.IsAny<SemanticLogLevel>(), It.IsAny<string>())).Callback(
                new Action<SemanticLogLevel, string>((level, message) =>
                {
                    output = message;
                }));

            log = new Lazy<SemanticLog>(() => new SemanticLog(channels, appenders, () => new JsonLogWriter()));
        }

        [Fact]
        public void Should_log_timestamp()
        {
            appenders.Add(new TimestampLogAppender(() => 1500));

            Log.LogFatal(w => {});

            var expected = 
                MakeTestCall(w => w
                    .WriteProperty("logLevel", "Fatal")
                    .WriteProperty("timestamp", 1500));

            Assert.Equal(expected, output);
        }

        [Fact]
        public void Should_log_values_with_appender()
        {
            appenders.Add(new ConstantsLogWriter(w => w.WriteProperty("logValue", 1500)));

            Log.LogFatal(m => { });

            var expected =
                MakeTestCall(w => w
                    .WriteProperty("logLevel", "Fatal")
                    .WriteProperty("logValue", 1500));

            Assert.Equal(expected, output);
        }

        [Fact]
        public void Should_log_application_info()
        {
            appenders.Add(new ApplicationInfoLogAppender(GetType().GetTypeInfo().Assembly));

            Log.LogFatal(m => { });

            var expected =
                MakeTestCall(w => w
                    .WriteProperty("logLevel", "Fatal")
                    .WriteProperty("applicationName", "Squidex.Infrastructure.Tests")
                    .WriteProperty("applicationVersion", "1.0.0.0"));

            Assert.Equal(expected, output);
        }

        [Fact]
        public void Should_log_with_trace()
        {
            Log.LogTrace(w => w.WriteProperty("logValue", 1500));

            var expected =
                MakeTestCall(w => w
                    .WriteProperty("logLevel", "Trace")
                    .WriteProperty("logValue", 1500));

            Assert.Equal(expected, output);
        }

        [Fact]
        public void Should_log_with_debug()
        {
            Log.LogDebug(w => w.WriteProperty("logValue", 1500));

            var expected =
                MakeTestCall(w => w
                    .WriteProperty("logLevel", "Debug")
                    .WriteProperty("logValue", 1500));

            Assert.Equal(expected, output);
        }

        [Fact]
        public void Should_log_with_information()
        {
            Log.LogInformation(w => w.WriteProperty("logValue", 1500));

            var expected =
                MakeTestCall(w => w
                    .WriteProperty("logLevel", "Information")
                    .WriteProperty("logValue", 1500));

            Assert.Equal(expected, output);
        }

        [Fact]
        public void Should_log_with_warning()
        {
            Log.LogWarning(w => w.WriteProperty("logValue", 1500));

            var expected =
                MakeTestCall(w => w
                    .WriteProperty("logLevel", "Warning")
                    .WriteProperty("logValue", 1500));

            Assert.Equal(expected, output);
        }

        [Fact]
        public void Should_log_with_warning_exception()
        {
            var exception = new InvalidOperationException();

            Log.LogWarning(exception);

            var expected =
                MakeTestCall(w => w
                    .WriteProperty("logLevel", "Warning")
                    .WriteException(exception));

            Assert.Equal(expected, output);
        }

        [Fact]
        public void Should_log_with_error()
        {
            Log.LogError(w => w.WriteProperty("logValue", 1500));

            var expected =
                MakeTestCall(w => w
                    .WriteProperty("logLevel", "Error")
                    .WriteProperty("logValue", 1500));

            Assert.Equal(expected, output);
        }

        [Fact]
        public void Should_log_with_error_exception()
        {
            var exception = new InvalidOperationException();

            Log.LogError(exception);

            var expected =
                MakeTestCall(w => w
                    .WriteProperty("logLevel", "Error")
                    .WriteException(exception));

            Assert.Equal(expected, output);
        }

        [Fact]
        public void Should_log_with_fatal()
        {
            Log.LogFatal(w => w.WriteProperty("logValue", 1500));

            var expected =
                MakeTestCall(w => w
                    .WriteProperty("logLevel", "Fatal")
                    .WriteProperty("logValue", 1500));

            Assert.Equal(expected, output);
        }

        [Fact]
        public void Should_log_with_fatal_exception()
        {
            var exception = new InvalidOperationException();

            Log.LogFatal(exception);

            var expected =
                MakeTestCall(w => w
                    .WriteProperty("logLevel", "Fatal")
                    .WriteException(exception));

            Assert.Equal(expected, output);
        }

        [Fact]
        public void Should_log_nothing_when_exception_is_null()
        {
            Log.LogFatal((Exception)null);

            var expected =
                MakeTestCall(w => w
                    .WriteProperty("logLevel", "Fatal"));

            Assert.Equal(expected, output);
        }

        [Fact]
        public void Should_measure_trace()
        {
            Log.MeasureTrace(w => w.WriteProperty("message", "My Message")).Dispose();

            var expected =
                MakeTestCall(w => w
                    .WriteProperty("logLevel", "Trace")
                    .WriteProperty("message", "My Message")
                    .WriteProperty("elapsedMs", 0));

            Assert.Equal(expected, output);
        }

        [Fact]
        public void Should_measure_debug()
        {
            Log.MeasureDebug(w => w.WriteProperty("message", "My Message")).Dispose();

            var expected =
                MakeTestCall(w => w
                    .WriteProperty("logLevel", "Debug")
                    .WriteProperty("message", "My Message")
                    .WriteProperty("elapsedMs", 0));

            Assert.True(output.StartsWith(expected.Substring(0, 55)));
        }

        [Fact]
        public void Should_measure_information()
        {
            Log.MeasureInformation(w => w.WriteProperty("message", "My Message")).Dispose();

            var expected =
                MakeTestCall(w => w
                    .WriteProperty("logLevel", "Information")
                    .WriteProperty("message", "My Message")
                    .WriteProperty("elapsedMs", 0));

            Assert.Equal(expected, output);
        }

        [Fact]
        public void Should_log_with_extensions_logger()
        {
            var exception = new InvalidOperationException();

            var loggerFactory = new LoggerFactory().AddSemanticLog(Log);
            var loggerInstance = loggerFactory.CreateLogger<SemanticLogTests>();

            loggerInstance.LogCritical(new EventId(123, "EventName"), exception, "Log {0}", 123);
            
            var expected =
                MakeTestCall(w => w
                    .WriteProperty("logLevel", "Fatal")
                    .WriteProperty("message", "Log 123")
                    .WriteObject("eventId", e => e
                        .WriteProperty("id", 123)
                        .WriteProperty("name", "EventName"))
                    .WriteException(exception)
                    .WriteProperty("category", "Squidex.Infrastructure.Log.SemanticLogTests"));

            Assert.Equal(expected, output);
        }

        private static string MakeTestCall(Action<IObjectWriter> writer)
        {
            IObjectWriter sut = new JsonLogWriter();

            writer(sut);

            return sut.ToString();
        }
    }
}
