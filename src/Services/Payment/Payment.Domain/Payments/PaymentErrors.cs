using BuildingBlocks.Common.Results;

namespace Payment.Domain.Payments;

public static class PaymentErrors
{
    public static readonly Error InvalidAmount =
        new("PAYMENT_INVALID_AMOUNT", "Amount 0 se zyada hona chahiye.");

    public static readonly Error NotFound =
        new("PAYMENT_NOT_FOUND", "Is order ki payment nahi mili.");

    public static readonly Error RefundFailed =
        new("PAYMENT_REFUND_FAILED", "Refund fail hua.");

    public static Error Declined(string reason) =>
        new("PAYMENT_DECLINED", $"Payment decline ho gayi: {reason}");
}
