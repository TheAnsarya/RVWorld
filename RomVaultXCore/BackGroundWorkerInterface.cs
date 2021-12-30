namespace RVXCore
{
    public class BgwText
    {
        public BgwText(string Text)
        {
            this.Text = Text;
        }

        public string Text { get; private set; }
    }

    public class BgwText2
    {
        public BgwText2(string Text)
        {
            this.Text = Text;
        }

        public string Text { get; private set; }
    }

    public class BgwText3
    {
        public BgwText3(string Text)
        {
            this.Text = Text;
        }

        public string Text { get; private set; }
    }

    public class BgwSetRange
    {
        public BgwSetRange(int MaxVal)
        {
            this.MaxVal = MaxVal;
        }

        public int MaxVal { get; private set; }
    }

    public class BgwSetRange2
    {
        public BgwSetRange2(int MaxVal)
        {
            this.MaxVal = MaxVal;
        }

        public int MaxVal { get; private set; }
    }

    public class BgwValue2
    {
        public BgwValue2(int Value)
        {
            this.Value = Value;
        }

        public int Value { get; private set; }
    }

    public class BgwRange2Visible
    {
        public BgwRange2Visible(bool Visible)
        {
            this.Visible = Visible;
        }

        public bool Visible { get; private set; }
    }

    public class BgwShowError
    {
        public BgwShowError(string filename, string error)
        {
            this.Error = error;
            this.Filename = filename;
        }

        public string Filename { get; private set; }
        public string Error { get; private set; }
    }

    public class BgwShowFix
    {
        public BgwShowFix(string fixDir, string fixZip, string fixFile, ulong? size, string dir, string sourceDir, string sourceZip, string sourceFile)
        {
            FixDir = fixDir;
            FixZip = fixZip;
            FixFile = fixFile;
            Size = size.ToString();
            Dir = dir;
            SourceDir = sourceDir;
            SourceZip = sourceZip;
            SourceFile = sourceFile;
        }

        public string FixDir { get; private set; }
        public string FixZip { get; private set; }
        public string FixFile { get; private set; }
        public string Size { get; private set; }
        public string Dir { get; private set; }
        public string SourceDir { get; private set; }
        public string SourceZip { get; private set; }
        public string SourceFile { get; private set; }
    }

    public class BgwShowFixError
    {
        public BgwShowFixError(string FixError)
        {
            this.FixError = FixError;
        }

        public string FixError { get; private set; }
    }
}
