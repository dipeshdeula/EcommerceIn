namespace Application.Common.Helper
{
    public static class DataHelper
    {
        public static readonly string[] Provinces =
        {
            "Koshi",
            "Madhesh",
            "Bagmati",
            "Gandaki",
            "Lumbini",
            "Karnali",
            "Sudurpashchim"
        };

        public static readonly string[] Cities =
        {
            "Kathmandu","Lalitpur","Bhaktapur","Pokhara","Biratnagar","Birgunj","Butwal","Dharan","Janakpur","Hetauda","Nepalgunj","Dhangadhi",
            "Itahari","Bharatpur","Tulsipur","Ghorahi","Bhimdatta (Mahendranagar)","Kirtipur","Tikapur","Rajbiraj","Gaur", "Kalaiya", "Siraha",
            "Inaruwa","Lahan","Panauti","Banepa","Dhankuta","Bardibas","Besisahar","Sandhikharka","Chainpur","Jaleshwor","Gaighat", "Damauli","Tansen",
            "Baglung","Amargadhi","Waling","Beni","Dipayal","Bhairahawa","Simara","Chandrapur","Lamahi","Bardiya","Parasi","Phidim","Ilam","Putalibazar",
            "Rampur","Melamchi","Khairahani","Madhyapur Thimi","Tokha","Kohalpur","Shivraj","Sunwal","Chautara","Suryabinayak","Godawari (Lalitpur)","Godawari (Kailali)",
            "Barahathawa","Bardaghat","Manma","Martadi","Charikot","Rukumkot","Jiri","Bajura","Diktel","Tumlingtar","Salleri"
        };

        // Optional helper method for validation
        public static bool IsValidProvince(string province) =>
            Provinces.Any(p => string.Equals(p, province, StringComparison.OrdinalIgnoreCase));

        public static bool IsValidCity(string city) =>
            Cities.Any(c => string.Equals(c, city, StringComparison.OrdinalIgnoreCase));
    }
}
