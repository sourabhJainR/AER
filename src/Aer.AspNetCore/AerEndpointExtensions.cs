using Aer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;

namespace Aer.AspNetCore;

public static class AerEndpointExtensions
{
    public static IResult Aer(this IResultExtensions _, object? value) =>
        Results.Text(AER.Serialize(value), "application/aer; profile=text", System.Text.Encoding.UTF8);

    public static IResult AerAi(this IResultExtensions _, object? value) =>
        Results.Text(AER.ToAi(value), "application/aer; profile=ai", System.Text.Encoding.UTF8);
}
