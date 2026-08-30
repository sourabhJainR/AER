using Aer;
using Microsoft.AspNetCore.Http;

namespace Aer.AspNetCore;

public static class AerResults
{
    public static IResult Text(object? value) =>
        Results.Text(AER.Serialize(value), "application/aer; profile=text", System.Text.Encoding.UTF8);

    public static IResult Ai(object? value) =>
        Results.Text(AER.ToAi(value), "application/aer; profile=ai", System.Text.Encoding.UTF8);
}
