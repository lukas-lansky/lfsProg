using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HtmlAgilityPack;
using Newtonsoft.Json;
using System.Net;

namespace lfs
{
    class Program
    {
        static void Main(string[] args)
        {
            string baseUri = "http://www.lfs.cz/denni-program.htm?trideni=kina&section=vse&datum=2012-07-";
            var events = new List<Event>();
            for (int day = 21; day <= 28; day++)
            {
                addEvents(events, baseUri + day, new DateTime(2012, 7, day));
            }

            foreach (var e in events)
            {
                e.TotalDuration = e.Films.Sum(f => f.Duration);
            }

            System.IO.File.WriteAllText("lfs2012", JsonConvert.SerializeObject(events));
        }

        static void addEvents(List<Event> events, string uri, DateTime day)
        {
            var htmlD = new HtmlDocument();
            htmlD.LoadHtml(getCachedWebDocument(uri));
            var currentBuilding = "";
            Event currentEvent = null;
            foreach (HtmlNode tr in htmlD.DocumentNode.SelectNodes("//table[@class='program']/tr"))
            {
                if (tr.Attributes.Any(att => att.Name == "class" && att.Value == "small"))
                    continue;
                else if (tr.Attributes.Any(att => att.Name == "class" && att.Value == "heading"))
                {
                    currentBuilding = tr.InnerText.Trim();
                    continue;
                }
                else
                {
                    var time = tr.SelectSingleNode("td[1]").InnerText.Trim();
                    if (!string.IsNullOrEmpty(time))
                    {
                        if (currentEvent != null)
                            events.Add(currentEvent);
                        currentEvent = new Event()
                        {
                            Building = currentBuilding,
                            Start = new DateTime(day.Year, day.Month, day.Day,
                                Convert.ToInt32(time.Split(':')[0]),
                                Convert.ToInt32(time.Split(':')[1]), 0),
                            Films = new List<Film>()
                        };
                    }

                    try
                    {
                        var tra = tr.SelectSingleNode("td[2]//a");
                        if (tra == null)
                        {
                            continue;
                        }
                        var f = new Film()
                        {
                            Id = tra.Attributes["href"].Value.Split('=')[1],
                            CzechName = tra.InnerText.Trim()
                        };
                        var detailsSpan = tr.SelectSingleNode(".//span[@class='small']");
                        if (detailsSpan.ChildNodes.Count > 2)
                        {
                            f.OriginalName = detailsSpan.ChildNodes[0].InnerText.Trim();
                            detailsSpan = detailsSpan.ChildNodes[2];
                        }

                        if (detailsSpan.InnerText.IndexOf('/') > -1)
                        {
                            var triple = detailsSpan.InnerText.Trim().Split('/');
                            f.Origin = triple[0].Trim();
                            f.Year = Convert.ToInt32(triple[1]);
                            var dur = triple[2].Replace('\'', ' ').Trim();
                            f.Duration = Convert.ToInt32(dur == "" ? "0" : dur);
                        }

                        var section = tr.SelectSingleNode("td[3]//acronym");
                        if (section != null)
                        {
                            f.Section = section.GetAttributeValue("title", "");
                        }

                        currentEvent.Films.Add(f);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
        }

        /// <summary>
        /// We do not want to spam the LFS server with lots of repeated request during 
        /// debug. So, this is an ultrasimple cache.
        /// </summary>
        static string getCachedWebDocument(string uri)
        {
            string hashs = uri.GetHashCode().ToString() + ".html";
            if (!System.IO.File.Exists(hashs))
                new WebClient().DownloadFile(uri, hashs);
            
            return System.IO.File.ReadAllText(hashs);
        }
    }

    class Event
    {
        public DateTime Start;
        public string Building = "";
        public List<Film> Films;
        public int TotalDuration;
        public string LongestFilmName;
    }

    class Film
    {
        public string Id = "";
        public string CzechName = "";
        public string OriginalName = "";
        public string Origin = "";
        public int Year = 1800;
        public int Duration = 0;
        public string Section = "";
    }
}
