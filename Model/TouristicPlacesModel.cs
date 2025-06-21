namespace Trevoria.Model
{
    public class TouristicPlacesModel
    {
        public string? name { get; set; }
        public string? type { get; set; }
        public float lat { get; set; }
        public float lon { get; set; }
        public string? osm_type { get; set; }
        public long osm_id { get; set; }
        public string? website { get; set; }
        public string? address { get; set; }
        public string? city { get; set; }
    }

}
