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
        ComboBox TitleFilter, ArtistFilter, CountryFilter, CompanyFilter;
        TextBox PriceFilterFrom, PriceFilterTo, YearFilterFrom, YearFilterTo;
        RadioButton LINQ, DOM, SAX;
        Button Search, Reload, Reset, Transform;

        // hardcoded paths
        string XMLSourceFile = "../../data.xml";
        string XSLTSourceFile = "../../transformationRules.xsl";
        string HTMLTargetFile = "../../transformed.html";

        // logic elements
        CDFilter CurrentFilter = new CDFilter(); // stores filters values
        XMLParser Parser;

        Dictionary<string, bool> FirstTime = new Dictionary<string, bool>()
        {
            { "Title", true },
            { "Artist", true },
            { "Company", true },
            { "Country", true },
            { "PriceFrom", true },
            { "PriceTo", true },
            { "YearFrom", true },
            { "YearTo", true },
        };

        Dictionary<string, string> DefaultValues = new Dictionary<string, string>()
        {
            { "Title", "Title" },
            { "Artist", "Artist" },
            { "Company", "Company" },
            { "Country", "Country" },
            { "PriceFrom", "Price: from" },
            { "PriceTo", "Price: to" },
            { "YearFrom", "Year: from" },
            { "YearTo", "Year: to" },
        };

        public MainForm()
        {
            InitializeComponent();

            ContentContainer = new RichTextBox
            {
                Location = new Point(30, 30),
                Size = new Size((Width - 120) / 2, ClientSize.Height - 70)
            };
            Controls.Add(ContentContainer);
            InitializeFilters();
            InitializeRadioButtons();
            InitializeControlButtons();

            SizeChanged += XMLDataVisualizator_SizeChanged;
            FormClosing += XMLDataVisualizator_Closing;

            Parser = new LINQParser(XMLSourceFile);
            FillVizualizator();
        }

        // GUI elements initializers
        void InitializeFilters()
        {
            TitleFilter = new ComboBox
            {
                Location = new Point(90 + ContentContainer.Width, 30),
                Size = new Size(ContentContainer.Width - 30, 100),
                Font = new Font("Verdana", 16),
                Text = DefaultValues["Title"],
                Name = "Title",
            };

            ArtistFilter = new ComboBox
            {
                Location = new Point(90 + ContentContainer.Width,
                TitleFilter.Bounds.Bottom + 30),
                Size = new Size(ContentContainer.Width - 30, 100),
                Font = new Font("Verdana", 16),
                Text = DefaultValues["Artist"],
                Name = "Artist",
            };

            CountryFilter = new ComboBox
            {
                Location = new Point(90 + ContentContainer.Width,
                ArtistFilter.Bounds.Bottom + 30),
                Size = new Size(ContentContainer.Width - 30, 100),
                Font = new Font("Verdana", 16),
                Text = DefaultValues["Country"],
                Name = "Country",
            };

            CompanyFilter = new ComboBox
            {
                Location = new Point(90 + ContentContainer.Width,
                CountryFilter.Bounds.Bottom + 30),
                Size = new Size(ContentContainer.Width - 30, 100),
                Font = new Font("Verdana", 16),
                Text = DefaultValues["Company"],
                Name = "Company",
            };

            PriceFilterFrom = new TextBox
            {
                Location = new Point(90 + ContentContainer.Width,
                CompanyFilter.Bounds.Bottom + 30),
                Size = new Size((ContentContainer.Width - 70) / 2, 100),
                Font = new Font("Verdana", 14),
                Text = DefaultValues["PriceFrom"],
                Name = "PriceFrom",
            };

            PriceFilterTo = new TextBox
            {
                Location = new Point((190 + 3 * ContentContainer.Width) / 2,
                CompanyFilter.Bounds.Bottom + 30),
                Size = new Size((ContentContainer.Width - 70) / 2, 100),
                Font = new Font("Verdana", 14),
                Text = DefaultValues["PriceTo"],
                Name  = "PriceTo",
            };

            YearFilterFrom = new TextBox
            {
                Location = new Point(90 + ContentContainer.Width,
                PriceFilterTo.Bounds.Bottom + 20),
                Size = new Size((ContentContainer.Width - 70) / 2, 100),
                Font = new Font("Verdana", 14),
                Text = DefaultValues["YearFrom"],
                Name = "YearFrom",
            };

            YearFilterTo = new TextBox
            {
                Location = new Point((190 + 3 * ContentContainer.Width) / 2,
                PriceFilterTo.Bounds.Bottom + 20),
                Size = new Size((ContentContainer.Width - 70) / 2, 100),
                Font = new Font("Verdana", 14),
                Text = DefaultValues["YearTo"],
                Name = "YearTo",
            };
            
            TitleFilter.TextChanged += Filter_TextChanged;
            CompanyFilter.TextChanged += Filter_TextChanged;
            CountryFilter.TextChanged += Filter_TextChanged;
            ArtistFilter.TextChanged += Filter_TextChanged;
            YearFilterTo.TextChanged += Filter_TextChanged;
            YearFilterFrom.TextChanged += Filter_TextChanged;
            PriceFilterFrom.TextChanged += Filter_TextChanged;
            PriceFilterTo.TextChanged += Filter_TextChanged;

            TitleFilter.Enter += Filter_Enter;
            CompanyFilter.Enter += Filter_Enter;
            CountryFilter.Enter += Filter_Enter;
            ArtistFilter.Enter += Filter_Enter;
            YearFilterTo.Enter += Filter_Enter;
            YearFilterFrom.Enter += Filter_Enter;
            PriceFilterFrom.Enter += Filter_Enter;
            PriceFilterTo.Enter += Filter_Enter;

            TitleFilter.Leave += Filter_Leave;
            CompanyFilter.Leave += Filter_Leave;
            CountryFilter.Leave += Filter_Leave;
            ArtistFilter.Leave += Filter_Leave;
            YearFilterTo.Leave += Filter_Leave;
            YearFilterFrom.Leave += Filter_Leave;
            PriceFilterFrom.Leave += Filter_Leave;
            PriceFilterTo.Leave += Filter_Leave;

            Controls.Add(TitleFilter);
            Controls.Add(ArtistFilter);
            Controls.Add(CountryFilter);
            Controls.Add(CompanyFilter);
            Controls.Add(PriceFilterFrom);
            Controls.Add(PriceFilterTo);
            Controls.Add(YearFilterFrom);
            Controls.Add(YearFilterTo);
        }

        void InitializeRadioButtons()
        {
            LINQ = new RadioButton()
            {
                Text = "LINQ",
                Name = "LINQ",
                Checked = true,
                Font = new Font("Verdana", 12),
                Location = new Point(YearFilterFrom.Bounds.X, YearFilterFrom.Bounds.Bottom + 21),
            };
            DOM = new RadioButton()
            {
                Text = "DOM",
                Name = "DOM",
                Font = new Font("Verdana", 12),
                Location = new Point(LINQ.Bounds.Right, LINQ.Bounds.Y),
            };
            SAX = new RadioButton()
            {
                Text = "SAX",
                Name = "SAX",
                Font = new Font("Verdana", 12),
                Location = new Point(DOM.Bounds.Right, DOM.Bounds.Y),
            };

            LINQ.CheckedChanged += RadioButton_CheckedChanged;
            DOM.CheckedChanged += RadioButton_CheckedChanged;
            SAX.CheckedChanged += RadioButton_CheckedChanged;

            Controls.Add(LINQ);
            Controls.Add(DOM);
            Controls.Add(SAX);
        }

        void InitializeControlButtons()
        {
            Search = new Button()
            {
                Text = "Search",
                Location = new Point(TitleFilter.Bounds.X, LINQ.Bounds.Bottom + 21),
                Font = new Font("Verdana", 10),
                Size = new Size(70, 30),
            };
            Reload = new Button()
            {
                Text = "Reload data",
                Location = new Point(Search.Bounds.Right + 10, Search.Bounds.Y),
                Font = new Font("Verdana", 10),
                Size = new Size(115, 30),
            };
            Reset = new Button()
            {
                Text = "Reset filters",
                Location = new Point(Reload.Bounds.Right + 10, Reload.Bounds.Y),
                Font = new Font("Verdana", 10),
                Size = new Size(115, 30),
            };

            Search.Click += (s, e) => FillVizualizator();
            Reload.Click += (s, e) => ReloadFile();
            Reset.Click += (s, e) => ResetFilters();

            Transform = new Button()
            {
                Text = "Transform to HTML",
                Location = new Point(ContentContainer.Bounds.X +
                    (ContentContainer.Width - 160) / 2, ContentContainer.Bounds.Bottom + 5),
                Font = new Font("Verdana", 10),
                Size = new Size(160, 30),
            };

            Transform.Click += (s, e) => TransformToHTML();

            Controls.Add(Search);
            Controls.Add(Reload);
            Controls.Add(Reset);
            Controls.Add(Transform);
        }

        
        // event handlers
        void RadioButton_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton selectedTool = sender as RadioButton;
            if (selectedTool.Name == "LINQ") Parser = new LINQParser(XMLSourceFile);
            else if (selectedTool.Name == "DOM") Parser = new DOMParser(XMLSourceFile);
            //else if (selectedTool.Name == "SAX") Parser = new SAXParser(XMLSourceFile);
        }

        void Filter_Enter(object sender, EventArgs e)
        {
            if (sender is TextBox)
            {
                TextBox filter = sender as TextBox;
                if (DefaultValues[filter.Name] == filter.Text) filter.Text = "";
            }
            else if (sender is ComboBox) 
            {
                ComboBox filter = sender as ComboBox;
                if (DefaultValues[filter.Name] == filter.Text) filter.Text = "";
            }
        }

        void Filter_Leave(object sender, EventArgs e)
        {
            if (sender is TextBox)
            {
                TextBox filter = sender as TextBox;
                if (string.IsNullOrWhiteSpace(filter.Text)) 
                    filter.Text = DefaultValues[filter.Name];
            }
            else if (sender is ComboBox)
            {
                ComboBox filter = sender as ComboBox;
                if (string.IsNullOrWhiteSpace(filter.Text))
                    filter.Text = DefaultValues[filter.Name];
            }
        }

        void Filter_TextChanged(object sender, EventArgs e)
        {
            string newValue, propName;
            if (sender is TextBox)
            {
                TextBox f = sender as TextBox;
                newValue = f.Text;
                propName = f.Name;
                if (newValue == DefaultValues[propName]) return;
                CurrentFilter.SetFilter(propName, newValue);
            }
            else
            {
                ComboBox f = sender as ComboBox;
                newValue = f.Text;
                propName = f.Name;
                if (newValue == DefaultValues[propName]) return;
                CurrentFilter.SetFilter(propName, newValue);
            }
        }

        void XMLDataVisualizator_SizeChanged(object sender, EventArgs e)
        {
            ContentContainer.Size = new Size((Width - 120) / 2, ClientSize.Height - 70);

            TitleFilter.Location = new Point(90 + ContentContainer.Width, 30);
            TitleFilter.Size = new Size(ContentContainer.Width - 30, 100);

            ArtistFilter.Location = new Point(90 + ContentContainer.Width,
                TitleFilter.Bounds.Bottom + 30);
            ArtistFilter.Size = new Size(ContentContainer.Width - 30, 100);

            CountryFilter.Location = new Point(90 + ContentContainer.Width,
                ArtistFilter.Bounds.Bottom + 30);
            CountryFilter.Size = new Size(ContentContainer.Width - 30, 100);

            CompanyFilter.Location = new Point(90 + ContentContainer.Width,
                CountryFilter.Bounds.Bottom + 30);
            CompanyFilter.Size = new Size(ContentContainer.Width - 30, 100);

            PriceFilterFrom.Location = new Point(90 + ContentContainer.Width,
                CompanyFilter.Bounds.Bottom + 30);
            PriceFilterFrom.Size = new Size((ContentContainer.Width - 70) / 2, 100);

            PriceFilterTo.Location = new Point((190 + 3 * ContentContainer.Width) / 2,
                CompanyFilter.Bounds.Bottom + 30);
            PriceFilterTo.Size = new Size((ContentContainer.Width - 70) / 2, 100);

            YearFilterFrom.Location = new Point(90 + ContentContainer.Width,
                PriceFilterTo.Bounds.Bottom + 20);
            YearFilterFrom.Size = new Size((ContentContainer.Width - 70) / 2, 100);

            YearFilterTo.Location = new Point((190 + 3 * ContentContainer.Width) / 2,
                PriceFilterTo.Bounds.Bottom + 20);
            YearFilterTo.Size = new Size((ContentContainer.Width - 70) / 2, 100);

            LINQ.Location = new Point(YearFilterFrom.Bounds.X, YearFilterFrom.Bounds.Bottom + 21);
            DOM.Location = new Point(LINQ.Bounds.Right, LINQ.Bounds.Y);
            SAX.Location = new Point(DOM.Bounds.Right, DOM.Bounds.Y);

            Search.Location = new Point(TitleFilter.Bounds.X, LINQ.Bounds.Bottom + 21);
            Reload.Location = new Point(Search.Bounds.Right + 10, Search.Bounds.Y);
            Reset.Location = new Point(Reload.Bounds.Right + 10, Reload.Bounds.Y);

            Transform.Location = new Point(ContentContainer.Bounds.X +
                    (ContentContainer.Width - 160) / 2, ContentContainer.Bounds.Bottom + 5);
        }

        void XMLDataVisualizator_Closing(object sender, FormClosingEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to leave?", "Exit confirmation",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes)
                e.Cancel = true;
        }


        // logic implementation
        void FillVizualizator() // fills data depending on current filters
        {
            var res = Parser.FilterBy(CurrentFilter);
            ContentContainer.Text = 
                String.IsNullOrWhiteSpace(res.CDData)
                ? "No appropriate disks found."
                : res.CDData;

            TitleFilter.Items.Clear();
            ArtistFilter.Items.Clear();
            CountryFilter.Items.Clear();
            CompanyFilter.Items.Clear();
            
            TitleFilter.Items.AddRange(res.Titles.ToArray());
            ArtistFilter.Items.AddRange(res.Artists.ToArray());
            CountryFilter.Items.AddRange(res.Countries.ToArray());
            CompanyFilter.Items.AddRange(res.Companies.ToArray());
        }

        void ResetFilters()
        {
            CurrentFilter = new CDFilter();
            TitleFilter.Text = DefaultValues["Title"];
            ArtistFilter.Text = DefaultValues["Artist"];
            CompanyFilter.Text = DefaultValues["Company"];
            CountryFilter.Text = DefaultValues["Country"];
            PriceFilterFrom.Text = DefaultValues["PriceFrom"];
            PriceFilterTo.Text = DefaultValues["PriceTo"];
            YearFilterFrom.Text = DefaultValues["YearFrom"];
            YearFilterTo.Text = DefaultValues["YearTo"];
            FillVizualizator();
        }

        void ReloadFile()
        {
            Parser.Load(XMLSourceFile);
            FillVizualizator();
        }

        void TransformToHTML()
        {
            XslCompiledTransform transform = new XslCompiledTransform();
            transform.Load(XSLTSourceFile);
            transform.Transform(XMLSourceFile, HTMLTargetFile);
        }
    }
}