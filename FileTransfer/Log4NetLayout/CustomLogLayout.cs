using log4net.Layout;

namespace FileTransfer.Log4NetLayout
{
    public class CustomLogLayout : PatternLayout
    {
        public CustomLogLayout()
        {
            this.AddConverter("property", typeof(CustomLogPatternConveter));
        }
    }

}
