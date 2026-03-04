namespace Itm.Booking.Api.Dtos;

public record BookingRequest(
    int EventId,
    int Tickets,
    string? DiscountCode
);