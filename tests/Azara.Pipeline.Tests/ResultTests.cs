using Shouldly;

namespace Azara.Pipeline.Tests;

public class ResultTests
{
    [Fact]
    public void Success_ReportsIsSuccess()
    {
        var result = Result.Success();

        result.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
        result.IsCancelled.ShouldBeFalse();
    }

    [Fact]
    public void Failure_ReportsIsFailure_AndCarriesError()
    {
        var error = new Error("invalid_quantity", "Quantidade deve ser maior que zero.");

        var result = Result.Failure(error);

        result.IsFailure.ShouldBeTrue();
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe(error);
    }

    [Fact]
    public void Cancelled_IsNotTreatedAsFailure()
    {
        var result = Result.Cancelled();

        result.IsCancelled.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public void Success_AccessingError_Throws()
    {
        var result = Result.Success();

        Should.Throw<InvalidOperationException>(() => result.Error);
    }

    [Fact]
    public void GenericResult_Success_ExposesValue()
    {
        var result = Result<int>.Success(42);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
    }

    [Fact]
    public void GenericResult_Failure_AccessingValue_Throws()
    {
        var result = Result<int>.Failure(new Error("boom", "algo deu errado"));

        Should.Throw<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void GenericResult_Success_AccessingError_Throws()
    {
        var result = Result<int>.Success(1);

        Should.Throw<InvalidOperationException>(() => result.Error);
    }

    [Fact]
    public void GenericResult_ImplicitConversion_FromValue_CreatesSuccess()
    {
        Result<string> result = "ok";

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("ok");
    }

    [Fact]
    public void Equality_SameState_AreEqual()
    {
        Result.Success().ShouldBe(Result.Success());
        Result<int>.Success(1).ShouldBe(Result<int>.Success(1));

        var error = new Error("x", "y");
        Result.Failure(error).ShouldBe(Result.Failure(error));
    }

    [Fact]
    public void Equality_DifferentState_AreNotEqual()
    {
        Result.Success().ShouldNotBe(Result.Failure(new Error("x", "y")));
        Result<int>.Success(1).ShouldNotBe(Result<int>.Success(2));
    }
}
