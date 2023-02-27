using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;


namespace DaMaiGrabTicket
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("开始加载用户配置");
            UserSetting userSetting;
            try
            {
                userSetting = JsonConvert.DeserializeObject<UserSetting>(File.ReadAllText("appsettings.json"));
            }
            catch (Exception ex)
            {

                Console.WriteLine($"加载用户数据失败:{ex}");
                return;
            }

            var options = new ChromeOptions();
            options.AddArgument("--ignore-certificate-errors-spki-list");
            options.AddArgument("--ignore-certificate-errors");
            options.AddArgument("--ignore-ssl-errors");
            options.AddArgument("log-level=3");

            options.AddArgument(string.Format("--user-agent={0}", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/96.0.4664.110 Safari/537.11"));
            options.AddArgument("--disable-blink-features=AutomationControlled");

            //屏蔽浏览器被控制的提示
            options.AddExcludedArgument("enable-automation");
            options.AddAdditionalChromeOption("useAutomationExtension", false);


            ChromeDriver driver = new ChromeDriver(options);
            //窗口最大化，便于脚本执行
            driver.Manage().Window.Maximize();
            //设置超时等待(隐式等待)时间设置10秒
            //driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);

            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
            driver.Navigate().GoToUrl("https://passport.damai.cn/login");
            var login_box = wait.Until((driver) => driver.FindElement(By.Id("alibaba-login-box")));
            IWebDriver login = driver.SwitchTo().Frame(login_box);

            login.FindElement(By.Id("fm-login-id")).SendKeys(userSetting.UserName);
            login.FindElement(By.Id("fm-login-password")).SendKeys(userSetting.UserPwd);

            LoginCheck(driver, login);
            BuyTick(driver, userSetting.TargetUrl);






        }


        public static void LoginCheck(ChromeDriver driver, IWebDriver login)
        {
            bool slideSuccess = false;
            do
            {

                try
                {
                    if (!slideSuccess)
                    {
                        var dialog_content = new WebDriverWait(login, TimeSpan.FromSeconds(2)).Until((driver) => driver.FindElement(By.Id("baxia-dialog-content")));
                        if (dialog_content != null)
                        {
                            IWebDriver loginSideBox = driver.SwitchTo().Frame(dialog_content);
                            var btn_slide = loginSideBox.FindElement(By.ClassName("btn_slide"));
                            Actions actions = new Actions(driver);
                            actions.DragAndDropToOffset(btn_slide, 260, 0).Build().Perform();//单击并在指定的元素上按下鼠标按钮,然后移动到指定位置
                            slideSuccess = true;
                            loginSideBox.SwitchTo().ParentFrame();
                        }

                    }

                }
                catch (Exception)
                {
                }
                try
                {
                    WebDriverWait wait = new WebDriverWait(login, TimeSpan.FromSeconds(10));
                    var login_btn = wait.Until((driver) => driver.FindElement(By.ClassName("fm-btn")));
                    login_btn?.Click();

                }
                catch (Exception)
                {


                }




            } while (driver.Url.Contains("passport.damai.cn"));

        }

        public static void BuyTick(ChromeDriver driver, string url)
        {

            driver.Navigate().GoToUrl(url);
            try
            {
                driver.ExecuteScript("alert('请先选择好票的种类，然后在控制台上回车即可，不要点立即预定')");
            }
            catch (Exception)
            {


            }


            Console.Write("是否已经选择好票的种类，是否继续【回车即可】？：");
            Console.ReadLine();
            var buy_btn = new WebDriverWait(driver, TimeSpan.FromSeconds(5)).Until((driver) => driver.FindElement(By.ClassName("buybtn")));
            buy_btn?.Click();

            FillForm(driver);
        }
        public static void FillForm(ChromeDriver driver)
        {

            if (Slide(driver))
            {
                driver.ExecuteScript("window.location.reload()");
                Thread.Sleep(500);
            }

            try
            {
                var buyer_item = new WebDriverWait(driver, TimeSpan.FromSeconds(3)).Until((driver) => driver.FindElement(By.ClassName("buyer-list-item")));
                buyer_item?.Click();
            }
            catch (Exception ex)
            {

            }

            try
            {
                var next_btn = new WebDriverWait(driver, TimeSpan.FromSeconds(3)).Until((driver) => driver.FindElement(By.XPath("//*[@class='submit-wrapper']/button")));
                next_btn?.Click();
            }
            catch (Exception ex)
            {

            }
            Thread.Sleep(500);
            Slide(driver);
        }

        public static bool Slide(ChromeDriver driver)
        {

            bool isFor = false;
            bool isError = false;
            try
            {
                var slideFrame = new WebDriverWait(driver, TimeSpan.FromSeconds(5)).Until((driver) => driver.FindElement(By.Id("baxia-dialog-content")));
                if (slideFrame != null)
                {
                    isFor = true;
                    IWebDriver sideBox = driver.SwitchTo().Frame(slideFrame);
                    var btn_slide = sideBox.FindElement(By.ClassName("btn_slide"));
                    Actions actions = new Actions(driver);
                    actions.ClickAndHold(btn_slide).Perform();
                    //actions.MoveByOffset(158, 0).Perform();
                    var tracks = GetTracks(258);
                    foreach (var item in tracks)
                    {
                        actions.MoveByOffset(item, 0).Perform();
                    }
                    Thread.Sleep(200);
                    actions.Release().Perform();
                    Thread.Sleep(200);
                    try
                    {
                        var errloading = sideBox.FindElement(By.ClassName("errloading"));
                        if (errloading.Text.Contains("验证失败"))
                        {
                            errloading.Click();
                            isError = true;
                        }
                    }
                    catch (Exception)
                    {

                    }
                    sideBox.SwitchTo().ParentFrame();
                    if (isError) 
                    {
                       return Slide(driver);
                    }
                }
            }
            catch (Exception ex)
            {


            }
            return isFor;

        }
 
        public static List<int> GetTracks(double distance)
        {

            var tracks = new List<int>();
            double v = Random.Shared.Next(50,200);//初始速度
            Console.WriteLine(v);
            double current = 0;//当前位移
            double mid = distance * 4 / 5;
            int a = 0;
            double t = 0.31;
            while (current < distance)
            {
                if (current < mid)
                {
                    a = Random.Shared.Next(10, 50);
                }
                else
                {
                    a = Random.Shared.Next(1, 10);
                }
                double v0 = v;
                double s = v0 * t + 0.5 * a * Math.Pow(t, 2);

                current += s;//当前的位置

                tracks.Add((int)Math.Round(s));//添加到轨迹列表

                v = v0 + a * t;
            }

            return tracks;

        }
    }
}