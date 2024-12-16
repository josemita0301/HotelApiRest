using System.ComponentModel.DataAnnotations;

namespace HotelApiRest.Models
{
    public class Reservation
    {
        [Key]
        public int reservation_id { get; set; }
        public int room_number { get; set; }
        public string customer_name { get; set; }
        public DateTime start_date { get; set; }
        public DateTime end_date { get; set; }
        public string status { get; set; }
    }

}
