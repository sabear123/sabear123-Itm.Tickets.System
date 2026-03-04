namespace Itm.Event.Api.Dtos;

public record ReserveRequest(
    int EventId,
    int Quantity
);