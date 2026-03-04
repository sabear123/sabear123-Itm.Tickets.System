namespace Itm.Event.Api.Dtos;
public record EventDto(
    int Id,
    string Nombre,
    int PrecioBase,
    int SillasDisponibles
);