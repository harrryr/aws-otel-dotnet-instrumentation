using System.Diagnostics;
using static AWS.OpenTelemetry.AutoInstrumentation.AwsAttributeKeys;
using static OpenTelemetry.Trace.TraceSemanticConventions;
using static AWS.OpenTelemetry.AutoInstrumentation.AwsSpanProcessingUtil;
using System.Reflection;

namespace AWS.OpenTelemetry.AutoInstrumentation.Tests;

using Xunit;

public class AwsSpanProcessingUtilTest
{
    private readonly ActivitySource testSource = new ActivitySource("Test Source");
    private readonly string internalOperation = "InternalOperation";
    private readonly string unknownOperation = "UnknownOperation";
    private readonly string defaultPathValue = "/";

    public AwsSpanProcessingUtilTest()
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = (activitySource) => true,
            Sample = ((ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData)
        };
        ActivitySource.AddActivityListener(listener);
    }

    [Fact]
    public void TestGetIngressOperationValidName()
    {
        string validName = "ValidName";
        var spanDataMock = testSource.StartActivity("test", ActivityKind.Server);
        spanDataMock.DisplayName = validName;
        spanDataMock.Start();
        string actualOperation = AwsSpanProcessingUtil.GetIngressOperation(spanDataMock);
        Assert.Equal(validName, actualOperation);
    }

    [Fact]
    public void TestGetIngressOperationWithnotServer()
    {
        string validName = "ValidName";
        var spanDataMock = testSource.StartActivity("test", ActivityKind.Client);
        spanDataMock.DisplayName = validName;
        spanDataMock.Start();
        string actualOperation = AwsSpanProcessingUtil.GetIngressOperation(spanDataMock);
        Assert.Equal(internalOperation, actualOperation);
    }

    [Fact]
    public void TestGetIngressOperationHttpMethodNameAndNoFallback()
    {
        string validName = "GET";
        var spanDataMock = testSource.StartActivity("test", ActivityKind.Server);
        spanDataMock.SetTag(AttributeHttpRequestMethod, validName);
        spanDataMock.DisplayName = validName;
        spanDataMock.Start();
        string actualOperation = AwsSpanProcessingUtil.GetIngressOperation(spanDataMock);
        Assert.Equal(unknownOperation, actualOperation);
    }

    [Fact]
    public void TestGetIngressOperationEmptyNameAndNoFallback()
    {
        var spanDataMock = testSource.StartActivity("test", ActivityKind.Server);
        spanDataMock.DisplayName = "";
        spanDataMock.Start();
        string actualOperation = AwsSpanProcessingUtil.GetIngressOperation(spanDataMock);
        Assert.Equal(unknownOperation, actualOperation);
    }

    [Fact]
    public void TestGetIngressOperationUnknownNameAndNoFallback()
    {
        var spanDataMock = testSource.StartActivity("test", ActivityKind.Server);
        spanDataMock.DisplayName = unknownOperation;
        spanDataMock.Start();
        string actualOperation = AwsSpanProcessingUtil.GetIngressOperation(spanDataMock);
        Assert.Equal(unknownOperation, actualOperation);
    }

    [Fact]
    public void testGetIngressOperationInvalidNameAndValidTarget()
    {
        string invalidName = "";
        string validTarget = "/";
        var spanDataMock = testSource.StartActivity("test", ActivityKind.Server);
        spanDataMock.DisplayName = invalidName;
        spanDataMock.SetTag(AttributeUrlPath, validTarget);
        spanDataMock.Start();
        string actualOperation = AwsSpanProcessingUtil.GetIngressOperation(spanDataMock);
        Assert.Equal(validTarget, actualOperation);
    }


    [Fact]
    public void testGetIngressOperationInvalidNameAndValidTargetAndMethod()
    {
        string invalidName = "";
        string validTarget = "/";
        string validMethod = "GET";
        var spanDataMock = testSource.StartActivity("test", ActivityKind.Server);
        spanDataMock.DisplayName = invalidName;
        spanDataMock.SetTag(AttributeHttpRequestMethod, validMethod);
        spanDataMock.SetTag(AttributeUrlPath, validTarget);
        spanDataMock.Start();
        string actualOperation = AwsSpanProcessingUtil.GetIngressOperation(spanDataMock);
        Assert.Equal(validMethod + " " + validTarget, actualOperation);
    }

    [Fact]
    public void TestGetEgressOperationUseInternalOperation()
    {
        var spanDataMock = testSource.StartActivity("test", ActivityKind.Consumer);
        spanDataMock.DisplayName = "";
        spanDataMock.Start();
        string actualOperation = AwsSpanProcessingUtil.GetEgressOperation(spanDataMock);
        Assert.Equal(internalOperation, actualOperation);
    }

    [Fact]
    public void TestGetEgressOperationUseLocalOperation()
    {
        string operation = "TestOperation";
        var spanDataMock = testSource.StartActivity("test", ActivityKind.Server);
        spanDataMock.SetTag(AttributeAWSLocalOperation, operation);
        spanDataMock.Start();
        string actualOperation = AwsSpanProcessingUtil.GetEgressOperation(spanDataMock);
        Assert.Equal(operation, actualOperation);
    }

    [Fact]
    public void TestExtractAPIPathValueEmptyTarget()
    {
        string invalidTarget = "";
        string pathValue = AwsSpanProcessingUtil.ExtractAPIPathValue(invalidTarget);
        Assert.Equal(defaultPathValue, pathValue);
    }

    [Fact]
    public void TestExtractAPIPathValueNullTarget()
    {
        string invalidTarget = null;
        string pathValue = AwsSpanProcessingUtil.ExtractAPIPathValue(invalidTarget);
        Assert.Equal(defaultPathValue, pathValue);
    }

    [Fact]
    public void TestExtractAPIPathValueNoSlash()
    {
        string invalidTarget = "users";
        string pathValue = AwsSpanProcessingUtil.ExtractAPIPathValue(invalidTarget);
        Assert.Equal(defaultPathValue, pathValue);
    }

    [Fact]
    public void TestExtractAPIPathValueOnlySlash()
    {
        string invalidTarget = "/";
        string pathValue = AwsSpanProcessingUtil.ExtractAPIPathValue(invalidTarget);
        Assert.Equal(defaultPathValue, pathValue);
    }

    [Fact]
    public void TestExtractAPIPathValueOnlySlashAtEnd()
    {
        string invalidTarget = "users/";
        string pathValue = AwsSpanProcessingUtil.ExtractAPIPathValue(invalidTarget);
        Assert.Equal(defaultPathValue, pathValue);
    }

    [Fact]
    public void TestExtractAPIPathValidPath()
    {
        string validTarget = "/users/1/pet?query#fragment";
        string pathValue = AwsSpanProcessingUtil.ExtractAPIPathValue(validTarget);
        Assert.Equal("/users", pathValue);
    }

    [Fact]
    public void testIsKeyPresentKeyPresent()
    {
        var spanDataMock = testSource.StartActivity("test", ActivityKind.Server);
        spanDataMock.SetTag(AttributeUrlPath, "target");
        spanDataMock.Start();
        Assert.True(AwsSpanProcessingUtil.IsKeyPresent(spanDataMock, AttributeUrlPath));
    }

    [Fact]
    public void testIsKeyPresentKeyAbsent()
    {
        var spanDataMock = testSource.StartActivity("test", ActivityKind.Server);
        spanDataMock.Start();
        Assert.False(AwsSpanProcessingUtil.IsKeyPresent(spanDataMock, AttributeUrlPath));
    }

    [Fact]
    public void testIsAwsSpanTrue()
    {
        var spanDataMock = testSource.StartActivity("test", ActivityKind.Server);
        spanDataMock.SetTag(AttributeRpcSystem, "aws-api");
        spanDataMock.Start();
        Assert.True(AwsSpanProcessingUtil.IsAwsSDKSpan(spanDataMock));
    }

    [Fact]
    public void testIsAwsSpanFalse()
    {
        var spanDataMock = testSource.StartActivity("test", ActivityKind.Server);
        spanDataMock.Start();
        Assert.False(AwsSpanProcessingUtil.IsAwsSDKSpan(spanDataMock));
    }

    [Fact]
    public void testShouldUseInternalOperationFalse()
    {
        var spanDataMock = testSource.StartActivity("test", ActivityKind.Server);
        Assert.False(AwsSpanProcessingUtil.ShouldUseInternalOperation(spanDataMock));

        spanDataMock = testSource.StartActivity("test", ActivityKind.Consumer);
        spanDataMock.Start();
        using (var subActivity = testSource.StartActivity("test Child"))
        {
            subActivity.SetParentId(spanDataMock.TraceId, spanDataMock.SpanId);
            subActivity.Start();
            Assert.False(AwsSpanProcessingUtil.ShouldUseInternalOperation(spanDataMock));
        }
    }

    [Fact]
    public void testShouldGenerateServiceMetricAttributes()
    {
        var spanDataMock = testSource.StartActivity("test");
        spanDataMock.Start();
        using (var subActivity = testSource.StartActivity("test Child", ActivityKind.Server))
        {
            subActivity.SetParentId(spanDataMock.TraceId, spanDataMock.SpanId);
            subActivity.Start();
            Assert.True(AwsSpanProcessingUtil.ShouldGenerateServiceMetricAttributes(subActivity));
        }

        using (var subActivity = testSource.StartActivity("test Child", ActivityKind.Consumer))
        {
            subActivity.SetParentId(spanDataMock.TraceId, spanDataMock.SpanId);
            subActivity.Start();
            Assert.False(AwsSpanProcessingUtil.ShouldGenerateServiceMetricAttributes(subActivity));
        }

        using (var subActivity = testSource.StartActivity("test Child", ActivityKind.Internal))
        {
            subActivity.SetParentId(spanDataMock.TraceId, spanDataMock.SpanId);
            subActivity.Start();
            Assert.False(AwsSpanProcessingUtil.ShouldGenerateServiceMetricAttributes(subActivity));
        }

        using (var subActivity = testSource.StartActivity("test Child", ActivityKind.Producer))
        {
            subActivity.SetParentId(spanDataMock.TraceId, spanDataMock.SpanId);
            subActivity.Start();
            Assert.False(AwsSpanProcessingUtil.ShouldGenerateServiceMetricAttributes(subActivity));
        }

        using (var subActivity = testSource.StartActivity("test Child", ActivityKind.Client))
        {
            subActivity.SetParentId(spanDataMock.TraceId, spanDataMock.SpanId);
            subActivity.Start();
            Assert.False(AwsSpanProcessingUtil.ShouldGenerateServiceMetricAttributes(subActivity));
        }
    }

    [Fact]
    public void testShouldGenerateDependencyMetricAttributes()
    {
        using (var spanDataMock = testSource.StartActivity("test", ActivityKind.Server))
        {
            Assert.False(AwsSpanProcessingUtil.ShouldGenerateDependencyMetricAttributes(spanDataMock));
        }
        using (var spanDataMock = testSource.StartActivity("test", ActivityKind.Internal))
        {
            Assert.False(AwsSpanProcessingUtil.ShouldGenerateDependencyMetricAttributes(spanDataMock));
        }
        using (var spanDataMock = testSource.StartActivity("test", ActivityKind.Consumer))
        {
            Assert.True(AwsSpanProcessingUtil.ShouldGenerateDependencyMetricAttributes(spanDataMock));
        }
        using (var spanDataMock = testSource.StartActivity("test", ActivityKind.Producer))
        {
            Assert.True(AwsSpanProcessingUtil.ShouldGenerateDependencyMetricAttributes(spanDataMock));
        }
        using (var spanDataMock = testSource.StartActivity("test", ActivityKind.Client))
        {
            Assert.True(AwsSpanProcessingUtil.ShouldGenerateDependencyMetricAttributes(spanDataMock));
        }

        var parentSpan = testSource.StartActivity("test Parent");
        using (var spanDataMock = testSource.StartActivity("test", ActivityKind.Consumer))
        {
            spanDataMock.SetParentId(parentSpan.TraceId, parentSpan.SpanId);
            spanDataMock.SetTag(AttributeMessagingOperation, MessagingOperationValues.Process);
            spanDataMock.SetTag(AttributeAWSConsumerParentSpanKind, ActivityKind.Consumer.GetType().Name);
            Assert.False(AwsSpanProcessingUtil.ShouldGenerateDependencyMetricAttributes(spanDataMock));
        }

        using (var spanDataMock = testSource.StartActivity("test", ActivityKind.Consumer))
        {
            spanDataMock.SetTag(AttributeMessagingOperation, MessagingOperationValues.Process);
            spanDataMock.SetTag(AttributeAWSConsumerParentSpanKind, ActivityKind.Consumer.GetType().Name);
            Assert.False(AwsSpanProcessingUtil.ShouldGenerateDependencyMetricAttributes(spanDataMock));
        }
    }

    [Fact]
    public void testIsLocalRoot()
    {
        using (var spanDataMock = testSource.StartActivity("test"))
        {
            Assert.True(AwsSpanProcessingUtil.IsLocalRoot(spanDataMock));
        }
        var parentSpan = testSource.StartActivity("test Parent");
        using (var spanDataMock = testSource.StartActivity("test"))
        {
            spanDataMock.SetParentId(parentSpan.TraceId, parentSpan.SpanId);
            Assert.False(AwsSpanProcessingUtil.IsLocalRoot(spanDataMock));
        }

        using (var spanDataMock = testSource.StartActivity("test"))
        {
            spanDataMock.SetParentId(parentSpan.TraceId, parentSpan.SpanId);
            PropertyInfo propertyInfo = typeof(Activity).GetProperty("HasRemoteParent", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            MethodInfo setterMethodInfo = propertyInfo.GetSetMethod(true);
            setterMethodInfo.Invoke(spanDataMock, new object[] { true });
            Assert.True(AwsSpanProcessingUtil.IsLocalRoot(spanDataMock));
        }
        parentSpan.Dispose();
        using (var spanDataMock = testSource.StartActivity("test"))
        {
            PropertyInfo propertyInfo = typeof(Activity).GetProperty("HasRemoteParent", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            MethodInfo setterMethodInfo = propertyInfo.GetSetMethod(true);
            setterMethodInfo.Invoke(spanDataMock, new object[] { true });
            Assert.True(AwsSpanProcessingUtil.IsLocalRoot(spanDataMock));
        }
    }

    [Fact]
    public void testIsConsumerProcessSpanFalse()
    {
        var spanDataMock = testSource.StartActivity("test");
        Assert.False(AwsSpanProcessingUtil.IsConsumerProcessSpan(spanDataMock));
    }

    [Fact]
    public void testNoMetricAttributesForSqsConsumerSpanAwsSdk()
    {
        ActivitySource awsActivitySource = new ActivitySource("Amazon.AWS.AWSClientInstrumentation");
        var spanDataMock = awsActivitySource.StartActivity("SQS.ReceiveMessage", ActivityKind.Consumer);
        spanDataMock.SetTag(AttributeAWSServiceName, "SQS");
        spanDataMock.Start();
        Assert.False(AwsSpanProcessingUtil.ShouldGenerateServiceMetricAttributes(spanDataMock));
        Assert.False(AwsSpanProcessingUtil.ShouldGenerateDependencyMetricAttributes(spanDataMock));
    }

    [Fact]
    public void testMetricAttributesGeneratedForOtherInstrumentationSqsConsumerSpan()
    {
        var spanDataMock = testSource.StartActivity("SQS.ReceiveMessage", ActivityKind.Consumer);
        spanDataMock.SetTag(AttributeAWSServiceName, "SQS");
        spanDataMock.Start();
        Assert.True(AwsSpanProcessingUtil.ShouldGenerateServiceMetricAttributes(spanDataMock));
        Assert.True(AwsSpanProcessingUtil.ShouldGenerateDependencyMetricAttributes(spanDataMock));
    }

    [Fact]
    public void testNoMetricAttributesForAwsSdkSqsConsumerProcessSpan()
    {
        ActivitySource awsActivitySource = new ActivitySource("Amazon.AWS.AWSClientInstrumentation");
        var spanDataMock = awsActivitySource.StartActivity("SQS.ReceiveMessage", ActivityKind.Consumer);
        spanDataMock.SetTag(AttributeAWSServiceName, "SQS");
        spanDataMock.SetTag(AttributeMessagingOperation, MessagingOperationValues.Process);
        spanDataMock.Start();
        Assert.False(AwsSpanProcessingUtil.ShouldGenerateServiceMetricAttributes(spanDataMock));
        Assert.False(AwsSpanProcessingUtil.ShouldGenerateDependencyMetricAttributes(spanDataMock));
        spanDataMock.Dispose();

        spanDataMock = awsActivitySource.StartActivity("SQS.ReceiveMessage", ActivityKind.Consumer);
        spanDataMock.SetTag(AttributeAWSServiceName, "SQS");
        spanDataMock.SetTag(AttributeMessagingOperation, MessagingOperationValues.Receive);
        spanDataMock.Start();
        Assert.True(AwsSpanProcessingUtil.ShouldGenerateServiceMetricAttributes(spanDataMock));
        Assert.True(AwsSpanProcessingUtil.ShouldGenerateDependencyMetricAttributes(spanDataMock));
        spanDataMock.Dispose();
    }

    [Fact]
    public void testSqlDialectKeywordsOrder()
    {
        List<String> keywords = AwsSpanProcessingUtil.GetDialectKeywords();
        int prevKeywordLength = int.MaxValue;
        foreach (var keyword in keywords)
        {
            int currKeywordLength = keyword.Length;
            Assert.True(prevKeywordLength >= currKeywordLength);
            prevKeywordLength = currKeywordLength;
        }
    }

    [Fact]
    public void TestSqlDialectKeywordsMaxLength()
    {
        var keywords = AwsSpanProcessingUtil.GetDialectKeywords();
        foreach (var keyword in keywords)
        {
            Assert.True(AwsSpanProcessingUtil.MaxKeywordLength >= keyword.Length);
        }
    }
}