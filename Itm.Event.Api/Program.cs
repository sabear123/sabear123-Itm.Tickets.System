using Itm.Event.Api.Dtos;
using Microsoft.AspNetCore.Server.HttpSys; // Importamos el DTO para usarlo en el controlador

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build(); 

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(); 
    app.UseSwaggerUI(); 
}

var eventDB = new List<EventDto>
{
    new EventDto (1, "Concierto ITM", 50000, 100),
};

app.MapGet("/api/events/{id}", (int id) =>
{
    var evento = eventDB.FirstOrDefault(e => e.Id == id);
    return evento is not null ? Results.Ok(evento) : Results.NotFound();
});

app.MapPost("/api/events/reserve", (ReserveRequest req) =>
{
    Console.WriteLine($"Reserva solicitada para el evento {req.EventId} con cantidad {req.Quantity}");
    var evento = eventDB.FirstOrDefault(e => e.Id == req.EventId);
    if (evento is null)
    {
        return Results.NotFound(new { Error = $"Evento con ID {req.EventId} no encontrado." });
    }
    if (evento.SillasDisponibles < req.Quantity)
    {
        return Results.BadRequest(new { Error = $"No hay suficientes sillas disponibles para el evento. Sillas disponibles: {evento.SillasDisponibles}" });
    }
    var index = eventDB.IndexOf(evento);
    eventDB[index] = evento with { SillasDisponibles = evento.SillasDisponibles - req.Quantity };
    return Results.Ok(new { Message = "¡Disfruta el concierto ITM!", NewSillasDisponibles = eventDB[index].SillasDisponibles });

});

app.MapPost("/api/events/release", (ReserveRequest req) =>
{
    Console.WriteLine($"[COMPENSACIÓN] Liberación solicitada para el evento {req.EventId} con cantidad {req.Quantity}");
    var evento = eventDB.FirstOrDefault(e => e.Id == req.EventId);
    if (evento is null)
    {
        return Results.NotFound(new { Error = $"Evento con ID {req.EventId} no encontrado." });
    }
    var index = eventDB.IndexOf(evento);
    eventDB[index] = evento with { SillasDisponibles = evento.SillasDisponibles + req.Quantity };
    Console.WriteLine($"[COMPENSACIÓN] Se devolvieron {req.Quantity} sillas para el evento {evento.Nombre}. NUEVAS SILLAS DISPONIBLES: {eventDB[index].SillasDisponibles}.");
    return Results.Ok(new { Message = "Sillas liberadas exitosamente.", NewSillasDisponibles = eventDB[index].SillasDisponibles });
});

app.Run();

record EventDto(int Id, string Nombre, int PrecioBase, int SillasDisponibles);
record ReserveRequest(int EventId, int Quantity);
