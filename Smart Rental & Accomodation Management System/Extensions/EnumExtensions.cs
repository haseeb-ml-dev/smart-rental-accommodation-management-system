using System.Text;

namespace Smart_Rental___Accomodation_Management_System.Extensions
{
    public static class EnumExtensions
    {
        public static string Humanize(this Enum value)
        {
            var name = value.ToString();
            var sb = new StringBuilder(name.Length + 4);

            for (int i = 0; i < name.Length; i++)
            {
                if (i > 0 && char.IsUpper(name[i]) && !char.IsUpper(name[i - 1]))
                {
                    sb.Append(' ');
                }
                sb.Append(name[i]);
            }

            return sb.ToString();
        }
    }
}
