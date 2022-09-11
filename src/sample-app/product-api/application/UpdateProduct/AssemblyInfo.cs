using System.Runtime.CompilerServices;
using Amazon.Lambda.Core;

[assembly:InternalsVisibleTo("ProductApi.Test")]
// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]