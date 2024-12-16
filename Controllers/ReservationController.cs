using HotelApiRest.Data;
using HotelApiRest.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelApiRest.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReservationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ReservationController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Reservation
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var reservations = await _context.Reservations.ToListAsync();
            return Ok(reservations);
        }

        // GET: api/Reservation/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null)
            {
                return NotFound();
            }
            return Ok(reservation);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Reservation reservation)
        {
            // Verificar disponibilidad mediante SOAP antes de crear la reserva
            bool isAvailable = await CheckRoomAvailability(reservation.room_number, reservation.start_date, reservation.end_date);

            if (!isAvailable)
            {
                return BadRequest("La habitación no está disponible en las fechas seleccionadas.");
            }

            // Si está disponible, crear la reserva
            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = reservation.reservation_id }, reservation);
        }

        // PUT: api/Reservation/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Reservation updatedReservation)
        {
            if (id != updatedReservation.reservation_id)
            {
                return BadRequest();
            }

            _context.Entry(updatedReservation).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Reservations.Any(r => r.reservation_id == id))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null)
            {
                return NotFound();
            }

            _context.Reservations.Remove(reservation);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        private async Task<bool> CheckRoomAvailability(int roomId, DateTime startDate, DateTime endDate)
        {
            var soapEnvelope = $@"
        <soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/' xmlns:tem='http://tempuri.org/'>
           <soapenv:Header/>
           <soapenv:Body>
              <tem:GetAvailableRooms>
                 <tem:roomType></tem:roomType>
                 <tem:status>disponible</tem:status>
                 <tem:startDate>{startDate:yyyy-MM-dd}</tem:startDate>
                 <tem:endDate>{endDate:yyyy-MM-dd}</tem:endDate>
              </tem:GetAvailableRooms>
           </soapenv:Body>
        </soapenv:Envelope>";

            using (var httpClient = new HttpClient())
            {
                var httpContent = new StringContent(soapEnvelope, System.Text.Encoding.UTF8, "text/xml");

                // Dirección del servicio SOAP
                var response = await httpClient.PostAsync("http://localhost:5000/AvailabilityService.asmx", httpContent);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();

                    // Verificar si el XML contiene la habitación disponible (por ejemplo, roomId)
                    return responseContent.Contains($"<RoomId>{roomId}</RoomId>");
                }

                return false;
            }
        }

    }
}
