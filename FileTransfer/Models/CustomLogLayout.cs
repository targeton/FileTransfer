using log4net.Layout;

namespace FileTransfer.Models
{
    public class CustomLogLayout : PatternLayout
    {
        public CustomLogLayout()
        {
            this.AddConverter("property", typeof(CustomLogPatternConveter));
        }
    }

}
