using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;

namespace XMLParsing
{
    public static class FilterTools
    {
        public static int LINQ { get; set; } = 0;
        public static int DOM { get; set; } = 1;
        public static int SAX { get; set; } = 2;
    }

    public class CDInfo 
    {
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Country { get; set; }
        public string Company { get; set; }
        public string Price { get; set; }
        public string Year { get; set; }

        public string InfoToDisplay(int cnt)
        {
            return
                $"CD #{cnt}\n" +
                $"Title: {Title}\n" +
                $"Artist: {Artist}\n" +
                $"Country: {Country}\n" +
                $"Company: {Company}\n" +
                $"Price: {Price}\n" +
                $"Year: {Year}\n";
        }
    }

    public class CDFilter 
    {
        public string Title { get; set; } = string.Empty;
        public string Artist { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public float PriceFrom { get; set; } = float.MinValue;
        public float PriceTo { get; set; } = float.MaxValue;
        public float YearFrom { get; set; } = float.MinValue;
        public float YearTo { get; set; } = float.MaxValue;

        static class DefaultFilterValues
        {
            public static class InvalidFilter
            {
                public static int YearFrom { get; } = int.MaxValue;
                public static int YearTo { get; } = int.MinValue;
                public static float PriceFrom { get; } = float.MaxValue;
                public static float PriceTo { get; } = float.MinValue;
            }

            public static class EmptyFilter
            {
                public static int YearFrom { get; } = int.MinValue;
                public static int YearTo { get; } = int.MaxValue;
                public static float PriceFrom { get; } = float.MinValue;
                public static float PriceTo { get; } = float.MaxValue;
            }
        }

        static NumberFormatInfo nfi = new NumberFormatInfo
        {
            NumberDecimalSeparator = "."
        };
        abstract class FilterException : ArgumentException
        {
            public FilterException() : base()
            {
            }
        }
        class FilterEmptyException : FilterException
        {
            public FilterEmptyException() : base()
            {
            }
        }
        class FilterInvalidException : FilterException
        {
            public FilterInvalidException() : base()
            {
            }
        }

        float ConvertFilterToFloat(string num)
        {
            if (string.IsNullOrWhiteSpace(num))
            {
                throw new FilterEmptyException();
            }
            try
            {
                return Convert.ToSingle(num, nfi);
            }
            catch
            {
                throw new FilterInvalidException();
            }
        }
        
        public void SetFilter(string name, string value)
        {
            PropertyInfo filterToSet = GetType().GetProperty(name);
            Type filterType = filterToSet.PropertyType;
            if (filterType == typeof(string))
            {
                filterToSet.SetValue(this, value.Trim().ToLower());
            }
            else 
            {
                try
                {
                    float converted = ConvertFilterToFloat(value);
                    filterToSet.SetValue(this, converted);
                }
                catch (FilterEmptyException)
                {
                    filterToSet.SetValue(this, 
                        typeof(DefaultFilterValues.EmptyFilter).GetProperty(name)
                        .GetValue(this));
                }
                catch(FilterInvalidException)
                {
                    filterToSet.SetValue(this,
                        typeof(DefaultFilterValues.InvalidFilter).GetProperty(name)
                        .GetValue(this));
                }
            }
        }

        public bool IsMatch(CDInfo candidate)
        {
            return (
                candidate.Title.ToLower().Contains(Title)
                && candidate.Artist.ToLower().Contains(Artist)
                && candidate.Company.ToLower().Contains(Company)
                && candidate.Country.ToLower().Contains(Country)
                && (Convert.ToSingle(candidate.Price, nfi) >= PriceFrom)
                && (Convert.ToSingle(candidate.Price, nfi) <= PriceTo)
                && (Convert.ToSingle(candidate.Year, nfi) >= YearFrom)
                && (Convert.ToSingle(candidate.Year, nfi) <= YearTo)
                );
        }
    }

    public class ResultData
    {
        public string CDData { get; set; } = string.Empty;
        public string[] Titles { get; set; }
        public string[] Artists { get; set; }
        public string[] Countries { get; set; }
        public string[] Companies { get; set; }
    }

    public interface XMLParser
    {
        ResultData FilterBy(CDFilter filter);

        void Load(string file);
    }

    public class LINQParser : XMLParser
    {
        List<CDInfo> CDs;

        public LINQParser(string file)
        {
            Load(file);
        }

        public void Load(string file)
        {
            XDocument XMLData = XDocument.Load(file);
            CDs = new List<CDInfo>(
                from cd in XMLData.Element("catalog").Elements("cd")
                select new CDInfo()
                {
                    Title = cd.Element("title").Value,
                    Artist = cd.Element("artist").Value,
                    Company = cd.Element("company").Value,
                    Country = cd.Element("country").Value,
                    Price = cd.Element("price").Value,
                    Year = cd.Element("year").Value,
                });
        }

        public ResultData FilterBy(CDFilter filter)
        {
            List<CDInfo> match = new List<CDInfo>(
                from cd in CDs
                where filter.IsMatch(cd)
                select cd);
            string dataToDisplay = "";
            for (int i = 0; i < match.Count(); i++)
            {
                dataToDisplay += match[i].InfoToDisplay(i + 1) + '\n';
            }

            return new ResultData()
            {
                CDData = dataToDisplay,
                Titles = (from cd in match
                    select cd.Title)
                    .Distinct().ToArray(),
                Artists = (from cd in match
                    select cd.Artist)
                    .Distinct().ToArray(),
                Countries = (from cd in match
                    select cd.Country)
                    .Distinct().ToArray(),
                Companies = (from cd in match
                    select cd.Company)
                    .Distinct().ToArray(),
            };
        }
    }

    public class DOMParser : XMLParser
    {
        XmlDocument xmlDoc = new XmlDocument();

        public DOMParser(string file)
        {
            Load(file);
        }

        public ResultData FilterBy(CDFilter filter)
        {
            var cdNodes = xmlDoc.SelectNodes("//cd");
            
            var dataToDisplay = "";
            List<string> titles = new List<string>();
            List<string> artists = new List<string>();
            List<string> companies = new List<string>();
            List<string> countries = new List<string>();
            int i = 0;
            foreach (XmlNode node in cdNodes)
            {
                CDInfo cd = new CDInfo()
                {
                    Title = node.SelectSingleNode("title").InnerText,
                    Artist = node.SelectSingleNode("artist").InnerText,
                    Company = node.SelectSingleNode("company").InnerText,
                    Country = node.SelectSingleNode("country").InnerText,
                    Price = node.SelectSingleNode("price").InnerText,
                    Year = node.SelectSingleNode("year").InnerText,
                };
                if (filter.IsMatch(cd))
                {
                    i++;
                    dataToDisplay += cd.InfoToDisplay(i) + '\n';
                    titles.Add(cd.Title);
                    artists.Add(cd.Artist);
                    companies.Add(cd.Company);
                    countries.Add(cd.Country);
                }
            }

            return new ResultData()
            {
                CDData = dataToDisplay,
                Titles = titles.Distinct().ToArray(),
                Artists = artists.Distinct().ToArray(),
                Companies = companies.Distinct().ToArray(),
                Countries = countries.Distinct().ToArray(),
            };
        }

        public void Load(string file)
        {
            xmlDoc.Load(file);
        }
    }

    public class SAXParser : XMLParser
    {
        XmlReader xmlReader;

        public SAXParser(string file)
        {
            Load(file);
        }       

        public ResultData FilterBy(CDFilter filter)
        {
            CDInfo cd = null;
            int i = 0;
            var dataToDisplay = "";
            List<string> titles = new List<string>();
            List<string> artists = new List<string>();
            List<string> companies = new List<string>();
            List<string> countries = new List<string>();

            while (xmlReader.Read())
            {
                switch (xmlReader.NodeType)
                {
                    case XmlNodeType.Element:
                        switch(xmlReader.Name)
                        {
                            case "cd":
                                cd = new CDInfo();
                                break;
                            case "title":
                                cd.Title = xmlReader.Value;
                                titles.Add(xmlReader.Value);
                                break;
                            case "artist":
                                cd.Artist = xmlReader.Value;
                                artists.Add(xmlReader.Value);
                                break;
                            case "company":
                                cd.Company = xmlReader.Value;
                                companies.Add(xmlReader.Value);
                                break;
                            case "country":
                                cd.Country = xmlReader.Value;
                                countries.Add(xmlReader.Value);
                                break;
                            case "price":
                                cd.Price = xmlReader.Value;
                                break;
                            case "year":
                                cd.Year = xmlReader.Value;
                                break;
                            default:
                                break;
                        }
                        break;
                    case XmlNodeType.EndElement:
                        if (xmlReader.Name == "cd")
                        {
                            if (filter.IsMatch(cd))
                            {
                                i++;
                                dataToDisplay += cd.InfoToDisplay(i) + '\n';
                            }
                        }
                        break;
                    default:
                        break;
                }
            }

            return new ResultData()
            {
                CDData = dataToDisplay,
                Titles = titles.Distinct().ToArray(),
                Artists = artists.Distinct().ToArray(),
                Companies = companies.Distinct().ToArray(),
                Countries = countries.Distinct().ToArray(),
            };
        }

        public void Load(string file)
        {
            xmlReader = XmlReader.Create(file);
        }
    }

    public partial class MainForm : Form
    {
        // GUI elements
        RichTextBox ContentContainer;
        ComboBox AuthorFilter, TitleFilter, DescriptionFilter, GenreFilter;
        TextBox PriceFromFilter, PriceToFilter, YearFromFilter, YearToFilter;
        RadioButton LINQ, DOM, SAX;
        Button Search, Reload, Reset, Transform;
        Label L1, L2, L3, L4, L5, L6, L7, L8;

        // hardcoded paths
        string XMLSourceFile = "../../../data.xml";
        string XSLTSourceFile = "../../../transformationRules.xsl";
        string HTMLTargetFile = "../../../transformed.html";

        // logic elements
        CDFilter CurrentFilter = new CDFilter(); // stores filters values
        XMLParser Parser;

        public MainForm()
        {
            InitializeComponent();

            ContentContainer = new RichTextBox
            {
                Location = new Point(30, 60),
                Size = new Size((Width - 120) / 2, ClientSize.Height - 80)
            };
            Controls.Add(ContentContainer);
            InitializeFilters();
            InitializeLabels();
            InitializeControlButtons();
            InitializeRadioButtons();

            SizeChanged += XMLDataVisualizator_SizeChanged;
            FormClosing += XMLDataVisualizator_Closing;

            Parser = new LINQParser(XMLSourceFile);
            FillVizualizator();
        }

        // GUI elements initializers
        private void InitializeFilters()
        {
            AuthorFilter = new ComboBox
            {
                Location = new Point(190 + ContentContainer.Width, 40),
                Size = new Size(ContentContainer.Width - 130, 100),
                Font = new Font("Verdana", 12),
                Name = "Author",
            };

            TitleFilter = new ComboBox
            {
                Location = new Point(190 + ContentContainer.Width,
                AuthorFilter.Bounds.Bottom + 20),
                Size = new Size(ContentContainer.Width - 130, 100),
                Font = new Font("Verdana", 12),
                Name = "Artist",
            };

            DescriptionFilter = new ComboBox
            {
                Location = new Point(190 + ContentContainer.Width,
                TitleFilter.Bounds.Bottom + 20),
                Size = new Size(ContentContainer.Width - 130, 100),
                Font = new Font("Verdana", 12),
                Name = "Country",
            };

            GenreFilter = new ComboBox
            {
                Location = new Point(190 + ContentContainer.Width,
                DescriptionFilter.Bounds.Bottom + 20),
                Size = new Size(ContentContainer.Width - 130, 100),
                Font = new Font("Verdana", 12),
                Name = "Company",
            };

            YearFromFilter = new TextBox
            {
                Location = new Point(190 + ContentContainer.Width,
                GenreFilter.Bounds.Bottom + 20),
                Size = new Size((AuthorFilter.Width - 30) / 2, 100),
                Font = new Font("Verdana", 10),
                Name = "YearFrom",
            };

            YearToFilter = new TextBox
            {
                Location =
                new Point(YearFromFilter.Right + 30,
                GenreFilter.Bounds.Bottom + 20),
                Size = new Size((AuthorFilter.Width - 30) / 2, 100),
                Font = new Font("Verdana", 10),
                Name = "YearTo",
            };

            PriceFromFilter = new TextBox
            {
                Location =
                new Point(190 + ContentContainer.Width,
                YearFromFilter.Bounds.Bottom + 20),
                Size = new Size((AuthorFilter.Width - 30) / 2, 100),
                Font = new Font("Verdana", 10),
                Name = "PriceFrom",
            };

            PriceToFilter = new TextBox
            {
                Location = new Point(YearFromFilter.Right + 30,
                YearFromFilter.Bounds.Bottom + 20),
                Size = new Size((AuthorFilter.Width - 30) / 2, 100),
                Font = new Font("Verdana", 10),
                Name  = "PriceTo",
            };
            
            AuthorFilter.TextChanged += FilterChanged;
            GenreFilter.TextChanged += FilterChanged;
            DescriptionFilter.TextChanged += FilterChanged;
            TitleFilter.TextChanged += FilterChanged;
            YearToFilter.TextChanged += FilterChanged;
            YearFromFilter.TextChanged += FilterChanged;
            PriceFromFilter.TextChanged += FilterChanged;
            PriceToFilter.TextChanged += FilterChanged;

            Controls.Add(AuthorFilter);
            Controls.Add(TitleFilter);
            Controls.Add(DescriptionFilter);
            Controls.Add(GenreFilter);
            Controls.Add(PriceFromFilter);
            Controls.Add(PriceToFilter);
            Controls.Add(YearFromFilter);
            Controls.Add(YearToFilter);
        }

        private void InitializeRadioButtons()
        {
            SAX = new RadioButton()
            {
                Text = "SAX",
                Name = "SAX",
                Font = new Font("Verdana", 12),
                Height = 30,
                Location = new Point(Search.Left + 10, Search.Bottom + 15),
            };
            DOM = new RadioButton()
            {
                Text = "DOM",
                Name = "DOM",
                Height = 30,

                Font = new Font("Verdana", 12),
                Location = new Point(SAX.Right, SAX.Top),
            };
            LINQ = new RadioButton()
            {
                Text = "LINQ",
                Name = "LINQ",
                Checked = true,
                Height = 30,

                Font = new Font("Verdana", 12),
                Location = new Point(DOM.Right, SAX.Top),
            };
            

            LINQ.CheckedChanged += RadioButton_CheckedChanged;
            DOM.CheckedChanged += RadioButton_CheckedChanged;
            SAX.CheckedChanged += RadioButton_CheckedChanged;

            Controls.Add(LINQ);
            Controls.Add(DOM);
            Controls.Add(SAX);
        }

        private void InitializeControlButtons()
        {
            Search = new Button()
            {
                Text = "Search",
                Location = new Point(L1.Left + 20, PriceFromFilter.Bottom + 30),
                Font = new Font("Verdana", 10),
                Size = new Size(80, 40),
            };
            Reset = new Button()
            {
                Text = "Reset",
                Location = new Point(Search.Bounds.Right + 30, Search.Bounds.Y),
                Font = new Font("Verdana", 10),
                Size = new Size(80, 40),
            };
            Reload = new Button()
            {
                Text = "Reload",
                Location = new Point(Reset.Bounds.Right + 30, Reset.Bounds.Y),
                Font = new Font("Verdana", 10),
                Size = new Size(100, 40),
            };

            Search.Click += (s, e) => FillVizualizator();
            Reload.Click += (s, e) => ReloadFile();
            Reset.Click += (s, e) => ResetFilters();

            Transform = new Button()
            {
                Text = "Convert to HTML",
                Location = new Point((ContentContainer.Width - 200) / 2 + 30, 20),
                Font = new Font("Verdana", 10),
                Size = new Size(200, 30),
            };

            Transform.Click += (s, e) => TransformToHTML();

            Controls.Add(Search);
            Controls.Add(Reload);
            Controls.Add(Reset);
            Controls.Add(Transform);
        }

        private void InitializeLabels()
        {
            L1 = new Label()
            {
                Text = "Author:",
                Location = new Point(ContentContainer.Width + 50, AuthorFilter.Top + 2),
                Width = 140,
                Font = new Font("Verdana", 12),
            };
            L2 = new Label()
            {
                Text = "Title:",
                Location = new Point(ContentContainer.Width + 50, TitleFilter.Top + 2),
                Width = 140,
                Font = new Font("Verdana", 12),
            };
            L3 = new Label()
            {
                Text = "Description:",
                Location = new Point(ContentContainer.Width + 50, DescriptionFilter.Top + 2),
                Width = 140,
                Font = new Font("Verdana", 12),
            };
            L4 = new Label()
            {
                Text = "Genre:",
                Location = new Point(ContentContainer.Width + 50, GenreFilter.Top + 2),
                Width = 140,
                Font = new Font("Verdana", 12),
            };
            L5 = new Label()
            {
                Text = "Published:",
                Location = new Point(ContentContainer.Width + 50, YearFromFilter.Top + 2),
                Width = 140,
                Font = new Font("Verdana", 12),
            };
            L6 = new Label()
            {
                Text = "Price:",
                Location = new Point(ContentContainer.Width + 50, PriceFromFilter.Top + 2),
                Width = 140,
                Font = new Font("Verdana", 12),
            };
            L7 = new Label()
            {
                Text = "-",
                Location = new Point(YearFromFilter.Right + 5, YearFromFilter.Top),
                Font = new Font("Verdana", 12),
            };
            L8 = new Label()
            {
                Text = "-",
                Location = new Point(PriceFromFilter.Right + 5, PriceFromFilter.Top),
                Font = new Font("Verdana", 12),
            };

            Controls.Add(L1);
            Controls.Add(L2);
            Controls.Add(L3);
            Controls.Add(L4);
            Controls.Add(L5);
            Controls.Add(L6);
            Controls.Add(L7);
            Controls.Add(L8);
        }
        
        // event handlers
        private void RadioButton_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton selectedTool = sender as RadioButton;
            if (selectedTool.Name == "LINQ") Parser = new LINQParser(XMLSourceFile);
            else if (selectedTool.Name == "DOM") Parser = new DOMParser(XMLSourceFile);
            //else if (selectedTool.Name == "SAX") Parser = new SAXParser(XMLSourceFile);
        }

        private void FilterChanged(object sender, EventArgs e)
        {
            string newValue, propName;
            if (sender is TextBox)
            {
                TextBox f = sender as TextBox;
                newValue = f.Text;
                propName = f.Name;
                CurrentFilter.SetFilter(propName, newValue);
            }
            else
            {
                ComboBox f = sender as ComboBox;
                newValue = f.Text;
                propName = f.Name;
                CurrentFilter.SetFilter(propName, newValue);
            }
        }

        private void XMLDataVisualizator_SizeChanged(object sender, EventArgs e)
        {
            ContentContainer.Size = new Size((Width - 120) / 2, ClientSize.Height - 80);

            AuthorFilter.Location = new Point(190 + ContentContainer.Width, 40);
            AuthorFilter.Size = new Size(ContentContainer.Width - 130, 100);

            TitleFilter.Location = new Point(190 + ContentContainer.Width,
                AuthorFilter.Bounds.Bottom + 20);
            TitleFilter.Size = new Size(ContentContainer.Width - 130, 100);

            DescriptionFilter.Location = new Point(190 + ContentContainer.Width,
                TitleFilter.Bounds.Bottom + 20);
             DescriptionFilter.Size = new Size(ContentContainer.Width - 130, 100);

            GenreFilter.Location = new Point(190 + ContentContainer.Width,
                DescriptionFilter.Bounds.Bottom + 20);
                GenreFilter.Size = new Size(ContentContainer.Width - 130, 100);

            YearFromFilter.Location = new Point(190 + ContentContainer.Width,
                GenreFilter.Bounds.Bottom + 20);
            YearFromFilter.Size = new Size((AuthorFilter.Width - 30) / 2, 100);

            YearToFilter.Location =
                new Point(YearFromFilter.Right + 30,
                GenreFilter.Bounds.Bottom + 20);
            YearToFilter.Size = new Size((AuthorFilter.Width - 30) / 2, 100);

            PriceFromFilter.Location =
                new Point(190 + ContentContainer.Width,
                YearFromFilter.Bounds.Bottom + 20);
            PriceFromFilter.Size = new Size((AuthorFilter.Width - 30) / 2, 100);

            PriceToFilter.Location = new Point(YearFromFilter.Right + 30,
                YearFromFilter.Bounds.Bottom + 20);
            PriceToFilter.Size = new Size((AuthorFilter.Width - 30) / 2, 100);

            L1.Location = new Point(ContentContainer.Width + 50, AuthorFilter.Top + 2);
            L2.Location = new Point(ContentContainer.Width + 50, TitleFilter.Top + 2);
            L3.Location = new Point(ContentContainer.Width + 50, DescriptionFilter.Top + 2);
            L4.Location = new Point(ContentContainer.Width + 50, GenreFilter.Top + 2);
            L5.Location = new Point(ContentContainer.Width + 50, YearFromFilter.Top + 2);
            L6.Location = new Point(ContentContainer.Width + 50, PriceFromFilter.Top + 2);
            L7.Location = new Point(YearFromFilter.Right + 5, YearFromFilter.Top);
            L8.Location = new Point(PriceFromFilter.Right + 5, PriceFromFilter.Top);

            Search.Location = new Point(AuthorFilter.Right - 320 - 
                (AuthorFilter.Right - L1.Left - 320) / 2, 
                PriceFromFilter.Bottom + 30);
            Reset.Location = new Point(Search.Bounds.Right + 30, Search.Bounds.Y);
            Reload.Location = new Point(Reset.Bounds.Right + 30, Reset.Bounds.Y);

            SAX.Location = new Point(Search.Left + 10, Search.Bottom + 15);
            DOM.Location = new Point(SAX.Right, SAX.Top);
            LINQ.Location = new Point(DOM.Right, SAX.Top);

            Transform.Location = new Point((ContentContainer.Width - 200) / 2 + 30, 20);
        }

        private void XMLDataVisualizator_Closing(object sender, FormClosingEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to leave?", "Exit confirmation",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes)
                e.Cancel = true;
        }


        // logic implementation
        private void FillVizualizator() // fills data depending on current filters
        {
            var res = Parser.FilterBy(CurrentFilter);
            ContentContainer.Text = 
                String.IsNullOrWhiteSpace(res.CDData)
                ? "Books not found :("
                : res.CDData;

            AuthorFilter.Items.Clear();
            TitleFilter.Items.Clear();
            DescriptionFilter.Items.Clear();
            GenreFilter.Items.Clear();
            
            AuthorFilter.Items.AddRange(res.Titles.ToArray());
            TitleFilter.Items.AddRange(res.Artists.ToArray());
            DescriptionFilter.Items.AddRange(res.Countries.ToArray());
            GenreFilter.Items.AddRange(res.Companies.ToArray());
        }

        private void ResetFilters()
        {
            CurrentFilter = new CDFilter();
            AuthorFilter.Text = "";
            TitleFilter.Text = "";
            GenreFilter.Text = "";
            DescriptionFilter.Text = ""; 
            PriceFromFilter.Text = "";
            PriceToFilter.Text = "";
            YearFromFilter.Text = "";
            YearToFilter.Text = "";
            FillVizualizator();
        }

        private void ReloadFile()
        {
            Parser.Load(XMLSourceFile);
            FillVizualizator();
        }

        private void TransformToHTML()
        {
            XslCompiledTransform transform = new XslCompiledTransform();
            transform.Load(XSLTSourceFile);
            transform.Transform(XMLSourceFile, HTMLTargetFile);
        }
    }
}