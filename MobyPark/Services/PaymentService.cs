using System.Text.Json;

using Microsoft.EntityFrameworkCore;

using MobyPark.Constants;
using MobyPark.Data;
using MobyPark.EncryptionHelper;
using MobyPark.Entities;
using MobyPark.Models;

namespace MobyPark.Services
{
    public class PaymentService(UserDbContext context, IEncryptionService encryption) : IPaymentService
    {
        public async Task<(PaymentReadDto? dto, string? error, int? status)> CompletePaymentAsync(Guid userId, string transactionId, PaymentValidationDto request)
        {
            if (userId == Guid.Empty)
                return (null, "User ID is required.", 400);

            if (string.IsNullOrWhiteSpace(transactionId))
                return (null, "Transaction ID is required.", 400);

            if (request is null)
                return (null, "Request body is required.", 400);

            if (string.IsNullOrWhiteSpace(request.Validation))
                return (null, "Validation field is missing.", 400);

            if (request.T_Data.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
                return (null, "t_data field is missing.", 400);

            var payment = await context.Payments
                .Include(p => p.Session)
                .Include(p => p.ParkingLot)
                .FirstOrDefaultAsync(p => p.Transaction == transactionId);

            if (payment is null || payment.UserId != userId)
                return (null, "Payment not found.", 404);

            if (payment.Completed is not null)
                return (null, "Payment is already completed.", 409);

            if (string.IsNullOrWhiteSpace(payment.Hash) ||
                !string.Equals(payment.Hash, request.Validation, StringComparison.Ordinal))
            {
                return (null, "Validation failed.", 401);
            }

            payment.Completed = DateTimeOffset.UtcNow;
            payment.T_Data = request.T_Data.GetRawText();

            if (payment.SessionId.HasValue)
            {
                var session = payment.Session
                              ?? await context.Sessions.FirstOrDefaultAsync(s => s.Id == payment.SessionId.Value);

                if (session is not null)
                    session.PaymentStatus = PaymentStatuses.Paid;
            }

            await context.SaveChangesAsync();

            return (MapToReadDto(payment), null, null);
        }

        private async Task<(PaymentReadDto? dto, string? error, int? status)> ApplyDiscount(PaymentsDto payment)
        {
            var paymentInDb = await context.Payments.FirstOrDefaultAsync(p => p.Transaction == payment.Transaction);

            if (paymentInDb is null)
                return (null, "Payment not found", 404);

            if (string.IsNullOrWhiteSpace(payment.DiscountCode))
            {
                paymentInDb.AmountWithDiscount = paymentInDb.Amount;
                await context.SaveChangesAsync();

                return (MapToReadDto(paymentInDb), "No discount applied", 200);
            }

            var discount = await context.Discounts
                .Where(d => d.Code == payment.DiscountCode.Trim())
                .FirstOrDefaultAsync();

            if (discount is null)
                return (null, $"Discount code {payment.DiscountCode} not found", 404);

            if (discount.MaxUsage is not null && discount.CurrentUsage >= discount.MaxUsage)
                return (null, $"Discount code {payment.DiscountCode} has reached its maximum usage", 422);

            decimal discountedCost;
            if (discount.Type == DiscountType.FixedAmount)
                discountedCost = Math.Max(0, paymentInDb.Amount - discount.Value);
            else
            {
                var percentage = Math.Clamp(discount.Value, 0, 100);
                discountedCost = Math.Max(0, paymentInDb.Amount - (paymentInDb.Amount * percentage / 100m));
            }

            paymentInDb.AmountWithDiscount = discountedCost;
            if (discount.MaxUsage != null)
                discount.CurrentUsage++;

            await context.SaveChangesAsync();
            return (MapToReadDto(paymentInDb), "Discount succesfully applied", 200);
        }

        public async Task<(PaymentReadDto? dto, string? error, int? status)> FulfillPaymentAsync(Guid userId, PaymentsDto paymentRequest)
        {
            var discountResponse = await ApplyDiscount(paymentRequest);

            if (discountResponse.status != 200)
                return discountResponse;

            if (userId == Guid.Empty)
                return (null, "User ID is required.", 400);

            if (paymentRequest is null)
                return (null, "Request body is required.", 400);

            if (string.IsNullOrWhiteSpace(paymentRequest.Transaction))
                return (null, "Transaction number is required.", 400);

            var payment = await context.Payments
                .Include(p => p.Session)
                .Include(p => p.ParkingLot)
                .Include(p => p.Discount)
                .FirstOrDefaultAsync(p =>
                    p.Transaction == paymentRequest.Transaction &&
                    p.Completed == null &&
                    p.UserId == userId);

            if (payment is null)
                return (null, "Payment not found.", 404);

            // Compare AmountWithDiscount to actual amount sent. 
            // AmountWithDiscount should be equal to amount without any discount.
            if (payment.AmountWithDiscount != paymentRequest.Amount)
                return (null, "Amount mismatch.", 409);

            payment.Completed = DateTimeOffset.UtcNow;

            if (payment.SessionId.HasValue)
            {
                var session = payment.Session
                              ?? await context.Sessions.FirstOrDefaultAsync(s => s.Id == payment.SessionId.Value);

                if (session is not null)
                    session.PaymentStatus = PaymentStatuses.Paid;
            }

            await context.SaveChangesAsync();

            return (MapToReadDto(payment), null, null);
        }

        public async Task<(List<PaymentReadDto>? dto, string? error, int? status)> GetPaymentsForUserAsync(Guid userId)
        {
            if (userId == Guid.Empty)
                return (null, "User ID is required.", 400);

            var payments = await context.Payments
                .AsNoTracking()
                .Include(p => p.Session)
                .Include(p => p.ParkingLot)
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.Created_At)
                .ToListAsync();

            return (payments.Select(MapToReadDto).ToList(), null, null);
        }

        public async Task<(List<PaymentReadDto>? dto, string? error, int? status)> GetPaymentsForAnyUserAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return (null, "Username is required.", 400);

            var user = await context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user is null)
                return (null, "User not found.", 404);

            var payments = await context.Payments
                .AsNoTracking()
                .Include(p => p.Session)
                .Include(p => p.ParkingLot)
                .Where(p => p.UserId == user.Id)
                .OrderByDescending(p => p.Created_At)
                .ToListAsync();

            return (payments.Select(MapToReadDto).ToList(), null, null);
        }

        public async Task<(bool dto, string? error, int? status)> DeletePaymentByTransactionId(string transactionId)
        {
            if (string.IsNullOrWhiteSpace(transactionId))
                return (false, "Transaction ID is required.", 400);

            var payment = await context.Payments.FirstOrDefaultAsync(p => p.Transaction == transactionId);
            if (payment is null)
                return (false, "Payment not found.", 404);

            context.Payments.Remove(payment);
            await context.SaveChangesAsync();

            return (true, null, null);
        }

        private PaymentReadDto MapToReadDto(Payment p)
        {
            var status = p.Completed is null ? PaymentStatuses.Pending : PaymentStatuses.Paid;

            PaymentSessionSummaryDto? sessionDto = null;
            if (p.Session is not null)
            {
                sessionDto = new PaymentSessionSummaryDto
                {
                    Id = p.Session.Id,
                    LicensePlate = LicensePlateProtector.DecryptNormalized(encryption, p.Session.LicensePlate),
                    Started = p.Session.Started,
                    Stopped = p.Session.Stopped,
                    DurationMinutes = p.Session.DurationMinutes,
                    Cost = p.Session.Cost,
                    PaymentStatus = p.Session.PaymentStatus
                };
            }

            return new PaymentReadDto
            {
                Transaction = p.Transaction,
                Amount = p.Amount,

                DiscountCode = p.DiscountCode ?? "",
                AmountWithDiscount = p.AmountWithDiscount,

                CreatedAt = p.Created_At,
                Completed = p.Completed,
                Status = status,

                SessionId = p.SessionId ?? Guid.Empty,
                ParkingLotId = p.ParkingLotId,
                ParkingLotName = p.ParkingLot?.Name ?? string.Empty,

                Session = sessionDto
            };
        }
    }
}
