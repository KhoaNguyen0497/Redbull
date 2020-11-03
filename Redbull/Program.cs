using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using Caliburn.Micro;
using WindowsInput;
using System.Threading;

namespace Redbull
{
    public class Dot
    {
        public int X { get; set; }
        public int Y { get; set; }
        public bool IsEndPoint { get; set; }

        public bool IsStartPoint { get; set; }
        public IWebElement Element { get; set; }
    }

    public class Program
    {
        public static int SleepTime = 30;
        public static IList<Dot> GetNeighbors(Dot dot, IList<IList<Dot>> grid)
        {
            IList<Dot> neighbors = new List<Dot>();

            neighbors.Add(grid.SelectMany(x => x).Where(x => x != null).FirstOrDefault(x => x.X == dot.X - 1 && x.Y == dot.Y));
            neighbors.Add(grid.SelectMany(x => x).Where(x => x != null).FirstOrDefault(x => x.X == dot.X + 1 && x.Y == dot.Y));
            neighbors.Add(grid.SelectMany(x => x).Where(x => x != null).FirstOrDefault(x => x.X == dot.X && x.Y == dot.Y + 1));
            neighbors.Add(grid.SelectMany(x => x).Where(x => x != null).FirstOrDefault(x => x.X == dot.X && x.Y == dot.Y - 1));

            return neighbors.Where(x => x != null).ToList();
        }
        public static IList<Dot> Try(Dot dot, IList<Dot> order, IList<IList<Dot>> grid, int maxItem)
        {
            // try adding it
            order.Add(dot);

            //check if its complete
            if (order.Distinct().Count() == maxItem && dot.IsEndPoint)
            {
                return order;
            }

            // get neighbors
            IList<Dot> neighbors = GetNeighbors(dot, grid).Where(x => !order.Contains(x)).ToList();

            // back track if theres nowhere to go to
            if (neighbors.Count == 0)
            {
                return null;
            }

            
            
            foreach (Dot neighbor in neighbors)
            {
                IList<Dot> temp = Try(neighbor, new List<Dot>(order), grid, maxItem);
                if (temp != null)
                {
                    return temp;
                }
        
            }

            // if it tries all of the neighbors and fails, remove the current dot
            return null;
        }

        public static void Play(IList<IList<Dot>> grid, Actions act)
        {
            Random random = new Random();
            IList<Dot> order = new List<Dot>();
            Dot startingDot = grid.SelectMany(x => x).First(x => x != null && x.IsStartPoint);
            var sim = new InputSimulator();

            order = Try(startingDot, order, grid, grid.SelectMany(x => x).Where(x => x != null).Count());

            foreach (Dot dot in order)
            {
                Point location = dot.Element.Location;
                sim.Mouse.MoveMouseTo((location.X + 40) * 65535 / 3440, (location.Y + 155) * 65535 / 1440);
                if (dot.IsStartPoint)
                {
                    sim.Mouse.LeftButtonDown();
                }
                else if (dot.IsEndPoint)
                {
                    sim.Mouse.LeftButtonUp();
                }


                Thread.Sleep(SleepTime);
            }

        }
        public static IList<IList<Dot>> BuildGrid(IList<Dot> dots)
        {
            IList<IList<Dot>> grid = new List<IList<Dot>>();

            int maxY = dots.Max(x => x.Y);
            int maxX = dots.Max(x => x.X);
            for (int i = 0; i <= maxY; i++)
            {
                List<Dot> row = new List<Dot>();
                for (int j = 0; j <= maxX; j++)
                {
                    row.Add(null);
                }
                grid.Add(row);
            }

            foreach (Dot dot in dots)
            {
                grid[dot.Y][dot.X] = dot;
            }

            return grid;
        }


        public static IList<Dot> ConvertDots(IList<IWebElement> elements, string startingLocationString)
        {
            IList<Dot> dots = new List<Dot>();

            foreach (var element in elements)
            {
                string locationString = element.GetAttribute("transform").Replace("translate", "").Replace("(", "").Replace(")", "");
                string[] locations = locationString.Split();
                int x = int.Parse(locations[0].Substring(0, locations[0].IndexOf('.')));
                int y = int.Parse(locations[1].Substring(0, locations[1].IndexOf('.')));


                dots.Add(new Dot
                {
                    X = x,
                    Y = y,
                    IsStartPoint = startingLocationString == locationString,
                    IsEndPoint = element.FindElement(By.TagName("circle")).GetAttribute("fill") != "rgba(0%,0%,0%,1)" && startingLocationString != locationString,
                    Element = element
                });
            }

            return dots;
        }

        public static void HandleButtons(IWebDriver driver)
        {
            IWebElement acceptCookiesButton = driver.FindElement(By.Id("onetrust-accept-btn-handler"));
            acceptCookiesButton.Submit();

            IWebElement playButton = driver.FindElement(By.ClassName("rb-button--primary"));
            playButton.Submit();
        }
        public static void Main(string[] args)
        {
            IWebDriver driver = new ChromeDriver();
            //Console.WriteLine("Enter speed, 15 min, 100 max:");
            driver.Navigate().GoToUrl("https://www.redbull.com/nz-en/cartoons/link-puzzle-gaming");
            HandleButtons(driver);
            IWebElement frame = driver.FindElement(By.ClassName("full-game__stage"));
            driver.SwitchTo().Frame(frame);

            while (true)
            {
                try
                {
                    IWebElement board = driver.FindElement(By.Id("board"));
                    IList<IWebElement> dots = board.FindElements(By.TagName("g"));
                    IList<Dot> points = ConvertDots(dots, board.FindElement(By.TagName("polyline")).GetAttribute("points").Replace(",", ""));

                    IList<IList<Dot>> grid = BuildGrid(points);

                    Actions act = new Actions(driver);

                    Play(grid, act);
                }
                catch
                {

                }
            }              
        }
    }
}
