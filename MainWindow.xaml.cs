﻿using System;
using System.Text.RegularExpressions;
using System.Windows;
using SPORTident;
using SPORTident.Communication;
using LiteDB;
using System.Linq;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Documents;
using System.Windows.Controls;
using System.Xml;
using System.IO;


namespace RocketDownload
{

    public partial class MainWindow : Window
    {
        public Reader reader;
        public string databaselocation = "";
        public int complete = 0;
        public MainWindow()
        {

            InitializeComponent();
            reader = new Reader
            {
                WriteBackupFile = true,
                BackupFileName = System.IO.Path.Combine(Environment.CurrentDirectory, $@"backup\{DateTime.Now:yyyy-MM-dd}_stamps.bak")
            };
            backupNo.IsEnabled = false;
            backupButton.IsEnabled = false;
            Download.IsEnabled = false;
            Results.IsEnabled = false;
            Entries.IsEnabled = false;
            Courses.IsEnabled = false;
            comboboxPortsList.Items.Add("Ports List");
            comboboxPortsList.SelectedIndex = 0;
            reader.DeviceConfigurationRead += new DeviceConfigurationReadEventHandler(reader_DeviceConfigurationRead);
            reader.InputDeviceChanged += new ReaderDeviceChangedEventHandler(reader_InputDeviceChanged);
            reader.InputDeviceStateChanged += new ReaderDeviceStateChangedEventHandler(reader_InputDeviceStateChanged);
            reader.CardRead += new SPORTident.DataReadCompletedEventHandler(reader_CardRead);
        }
        public class Entry
        {
            public int Id { get; set; }
            public string Sicard { get; set; }
            public string Name { get; set; }
            public string Course { get; set; }
            public string Class { get; set; }
            public string Club { get; set; }
            public string Start { get; set; }
            public string Time { get; set; }
            public bool Downloaded { get; set; }
        }
        public class Course
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Distance { get; set; }
            public string Climb { get; set; }
            public string[] ControlCodes { get; set; }

        }


        private void reader_DeviceConfigurationRead(object sender, StationConfigurationEventArgs e)
        {
           writeLogColor(listboxLog, "-----  Connected to Station  -----", "#0000ff");

        }

        private void reader_InputDeviceStateChanged(object sender, ReaderDeviceStateChangedEventArgs e)
        {
           // writeLogColorNL(listboxLog, "InputDeviceStateChanged: " + e.PreviousState + " => " + e.CurrentState, "#0000ff");
        }

        private void reader_InputDeviceChanged(object sender, ReaderDeviceChangedEventArgs e)
        {
            //writeLogColorNL(listboxLog, "InputDeviceChanged: " + e.PreviousDevice + " => " + e.CurrentDevice, "#0000ff");
        }



        private void clearSIDevices()
        {
            comboboxPortsList.Items.Clear();
        }

        private void getSIDevices()
        {

            ReaderDeviceInfo.GetAvailableDeviceList();
            foreach (ReaderDeviceInfo device in ReaderDeviceInfo.AvailableDevices)
            {
                comboboxPortsList.Items.Add(device);
            
            }
            if (comboboxPortsList.Items.Count > 0)
            {
                
                comboboxPortsList.SelectedIndex = 0;
            }
            else
            {
                comboboxPortsList.Items.Add("No Stations Found");
                comboboxPortsList.SelectedIndex = 0;
            }
        }

        private void connect()
        {
            ReaderDeviceInfo readerDeviceInfo = (ReaderDeviceInfo)(comboboxPortsList.SelectedItem);
            reader.InputDevice = readerDeviceInfo;
            reader.OutputDevice = new ReaderDeviceInfo(ReaderDeviceType.None);
            reader.OpenInputDevice();
            reader.OpenOutputDevice();

            comboboxPortsList.IsEnabled = false;
            buttonRefresh.IsEnabled = false;
            buttonConnect.IsEnabled = false;




        }

        private void disconnect()
        {
            comboboxPortsList.IsEnabled = true;
            buttonRefresh.IsEnabled = true;
            buttonConnect.IsEnabled = true;


            reader.CloseInputDevice();
        }

        private void buttonRefresh_Click(object sender, RoutedEventArgs e)
        {

            clearSIDevices();
            getSIDevices();
        }

        private void buttonConnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                connect();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void reader_CardRead(object sender, SportidentDataEventArgs e)
        {
            SportidentCard card = e.Cards[0];
            var startfull = Regex.Split(Convert.ToString(card.StartPunch), "000;")[1];
            var finishfull = Regex.Split(Convert.ToString(card.FinishPunch), "000;")[1];
            DateTime start = new DateTime(2000, 1, 1, Convert.ToInt32(startfull.Split(':')[0]), Convert.ToInt32(startfull.Split(':')[1]), Convert.ToInt32(startfull.Split(':')[2]));
            DateTime end = new DateTime(2000, 1, 1, Convert.ToInt16(finishfull.Split(':')[0]), Convert.ToInt16(finishfull.Split(':')[1]), Convert.ToInt16(finishfull.Split(':')[2]));
            var totaltime = end.Subtract(start);

            var errors = "";
            complete = 0;
            var i = 0;
            var j = 0;
            
            using (var db = new LiteDatabase(@databaselocation))
            {
                var done = 0;
                var entries = db.GetCollection<Entry>("entries");
                var courses = db.GetCollection<Course>("courses");
                var results = entries.Find(Query.EQ("Sicard", card.Siid));
                var resultlist = from result in results select result;
                string[] controllist = { };
                foreach (var result in resultlist)
                {
                    if (result.Course != null)
                    {
                        var results1 = courses.Find(Query.EQ("Name", result.Course));
                        var resultlist1 = from result1 in results1 select result1;
                        foreach (var result1 in resultlist1)
                        {
                            controllist = result1.ControlCodes;
                            break;
                        }
                        if (controllist.Length <= card.ControlPunchList.ToArray().Length)
                        {
                            foreach (var control in controllist)
                            {
                                if (i + 1 < controllist.Length && j + 1 < card.ControlPunchList.ToArray().Length)
                                {
                                    if (Convert.ToString(card.ControlPunchList.ElementAtOrDefault(j)).Split(';')[1] == controllist[i])
                                    {
                                     }

                                    else if (Convert.ToString(card.ControlPunchList.ElementAtOrDefault(j)).Split(';')[1] == controllist[i + 1])
                                    {
                                        errors += " M" + Convert.ToString(i + 1);
                                        i += 1;

                                    }
                                    else if (Convert.ToString(card.ControlPunchList.ElementAtOrDefault(j + 1)).Split(';')[1] == controllist[i])
                                    {
                                        j += 1;

                                    }


                                    else
                                    {
                                        errors += " W" + Convert.ToString(i + 1);

                                    }
                                }
                                else
                                {
                                    if (Convert.ToString(card.ControlPunchList.ElementAtOrDefault(j)).Split(';')[1] == controllist[i])
                                    {

                                    }

                                    else
                                    {
                                        errors += " W" + Convert.ToString(i + 1);

                                    }
                                }
                                if (i + 1 > controllist.Length)
                                {
                                    break;
                                }
                                if (j + 1 > card.ControlPunchList.ToArray().Length)
                                {
                                    break;
                                }
                                i += 1;
                                j += 1;
                            }
                        }
                        else
                        {
                            foreach (var punch in card.ControlPunchList)
                            {
                                if (i + 1 < controllist.Length && j + 1 < card.ControlPunchList.ToArray().Length)
                                {
                                    if (Convert.ToString(card.ControlPunchList.ElementAtOrDefault(j)).Split(';')[1] == controllist[i])
                                    {

                                    }

                                    else if (Convert.ToString(card.ControlPunchList.ElementAtOrDefault(j)).Split(';')[1] == controllist[i + 1])
                                    {
                                        errors += " M" + Convert.ToString(i + 1);
                                        i += 1;

                                    }
                                    else if (Convert.ToString(card.ControlPunchList.ElementAtOrDefault(j + 1)).Split(';')[1] == controllist[i])
                                    {
                                        j += 1;

                                    }


                                    else
                                    {
                                        errors += " W" + Convert.ToString(i + 1);

                                    }
                                }
                                else
                                {
                                    if (Convert.ToString(card.ControlPunchList.ElementAtOrDefault(j)).Split(';')[1] == controllist[i])
                                    {

                                    }

                                    else
                                    {
                                        errors += " W" + Convert.ToString(i + 1);

                                    }
                                }
                                if (i + 1 > controllist.Length)
                                {
                                    break;
                                }
                                if (j + 1 > card.ControlPunchList.ToArray().Length)
                                {
                                    break;
                                }
                                i += 1;
                                j += 1;


                            }
                            if (card.ControlPunchList.ToArray().Length - controllist.Length + 1 == 0)
                            {
                                errors += "M" + Convert.ToString(controllist.Length + card.ControlPunchList.ToArray().Length - controllist.Length + 1);
                            }
                            else
                            {
                                errors += "M" + Convert.ToString(controllist.Length + card.ControlPunchList.ToArray().Length - controllist.Length + 1) + "-" + controllist.Length;
                            }
                        }

                        var entry = new Entry { };

                        if (errors == "")
                        {

                            entry.Name = result.Name;
                            entry.Course = result.Course;
                            entry.Class = result.Class;
                            entry.Club = result.Club;
                            entry.Start = result.Start;
                            entry.Time = Convert.ToString(totaltime);
                            entry.Sicard = card.Siid;
                            entry.Downloaded = true;
                            entry.Id = result.Id;
                            writeLogColorNL(listboxLog, card.Siid + " - " + entry.Name + " - " + entry.Course + " - " + Convert.ToString(totaltime), "#00d807");


                        }
                        else
                        {
                            entry.Name = result.Name;
                            entry.Course = result.Course;
                            entry.Class = result.Class;
                            entry.Club = result.Club;
                            entry.Start = result.Start;
                            entry.Time = errors;
                            entry.Sicard = card.Siid;
                            entry.Downloaded = true;
                            entry.Id = result.Id;
                            writeLogColorNL(listboxLog, card.Siid + " - " + entry.Name + " - " + entry.Course + " - " + errors, "#ff0000");

                        }

                        entries.Update(entry);
                        done = 1;
                        break;
                    }
                    else
                    {
                        var entry = new Entry
                        {
                            Name = result.Name,
                            Class = result.Class,
                            Club = result.Club,
                            Start = result.Start,
                            Time = Convert.ToString(totaltime),
                            Sicard = card.Siid,
                            Downloaded = true,
                            Id = result.Id,
                        };

                        entries.Update(entry);
                        writeLogColorNL(listboxLog, card.Siid + " - " + entry.Name + " - " + entry.Course + " - " + Convert.ToString(totaltime), "Black");
                        done = 1;
                        break;
                    }
                }


                if (done == 0)
                {
                    var entry1 = new Entry
                    {
                        Name = card.PersonalData.FirstName + ' ' + card.PersonalData.LastName,
                        Course = "Unknown",
                        Class = "",
                        Club = "",
                        Start = "",
                        Time = Convert.ToString(totaltime),
                        Sicard = card.Siid,
                        Downloaded = true,
                    };
                    entries.Insert(entry1);
                    writeLogColorNL(listboxLog, card.Siid + " - " + card.PersonalData.FirstName + ' ' + card.PersonalData.LastName + " - " + Convert.ToString(totaltime), "Black");



                }




            }

        }

        private void Enter_Click(object sender, RoutedEventArgs e)
        {
            var name = textName.Text;
            var si = textSI.Text;
            var course = textCourse.Text;
            var club = textClub.Text;
            var clas = textClass.Text;
            var start = textStart.Text;
            using (var db = new LiteDatabase(@databaselocation))
            {
                var entries = db.GetCollection<Entry>("entries");
                var results = entries.Find(Query.EQ("Sicard", si));
                var resultlist = from result in results select result;
                var done = 0;

                foreach (var result in resultlist)
                {
                    var entry = new Entry
                    {
                        Name = name,
                        Course = course,
                        Class = clas,
                        Club = club,
                        Start = start,
                        Sicard = si,
                    };

                    entry.Time = result.Time;
                    entry.Downloaded = result.Downloaded;
                    entry.Id = result.Id;
                    entries.Update(entry);

                    done = 1;
                    break;
                }
                if (done == 0)
                {
                    var entry1 = new Entry
                    {
                        Name = name,
                        Course = course,
                        Class = clas,
                        Club = club,
                        Start = start,
                        Sicard = si,
                        Time = "",
                        Downloaded = false
                    };
                    entries.Insert(entry1);


                }

                textName.Text = "";
                textSI.Text = "";
                textCourse.Text = "";
                textClass.Text = "";
                textStart.Text = "";
                textClub.Text = "";
                textTime.Text = "";
            }
        }



        private void Search_Click(object sender, RoutedEventArgs e)
        {
            using (var db = new LiteDatabase(@databaselocation))
            {
                if (textName.Text == "")
                {
                    var entries = db.GetCollection<Entry>("entries");
                    var results = entries.Find(Query.EQ("Sicard", textSI.Text));
                    var resultlist = from result in results select result;
                    foreach (var result in resultlist)
                    {
                        textName.Text = result.Name;
                        textSI.Text = result.Sicard;
                        textCourse.Text = result.Course;
                        textClass.Text = result.Class;
                        textStart.Text = result.Start;
                        textClub.Text = result.Club;
                        textTime.Text = result.Time;
                    }
                }
                else if (textSI.Text == "")
                {
                    var entries = db.GetCollection<Entry>("entries");
                    var results = entries.Find(Query.Contains("Name", textName.Text));
                    var resultlist = from result in results select result;
                    foreach (var result in resultlist)
                    {
                        textName.Text = result.Name;
                        textSI.Text = result.Sicard;
                        textCourse.Text = result.Course;
                        textClass.Text = result.Class;
                        textStart.Text = result.Start;
                        textClub.Text = result.Club;
                        textTime.Text = result.Time;
                    }
                }


            }
        }

        private void refresh_Click(object sender, RoutedEventArgs e)
        {
            using (var db = new LiteDatabase(@databaselocation))
            {
                var course = "";
                resultsClear();
                var entries = db.GetCollection<Entry>("entries");
                var results = entries.Find(Query.EQ("Downloaded", true)).OrderBy(x => x.Course).ThenBy(x => x.Time);
                var resultlist = from result in results select result;

                foreach (var result in resultlist)
                {
                    if(result.Course != course)
                    {
                        if (result.Course == "") {
                            result.Course = "Unknown";
                        };
                        resultsLog(result.Course+":");
                        resultsLog(Convert.ToString("    " + result.Sicard + " - " + result.Name + " - " + result.Course + " - " + result.Time));
                        course = result.Course;
                    }
                    else
                    {
                        resultsLog(Convert.ToString("    "+result.Sicard + " - " + result.Name +" - " + result.Time));

                    }
                }
            }
        }

        private void browse_Click(object sender, RoutedEventArgs e)
        {

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.CheckFileExists = false;
            openFileDialog.CheckPathExists = false;
            openFileDialog.Title = "Open or Create a Database";
            openFileDialog.Filter = "Database File (*.db)|*.db|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                textDatabase.Text = openFileDialog.FileName;
                databaselocation = openFileDialog.FileName;
                Download.IsEnabled = true;
                Results.IsEnabled = true;
                Entries.IsEnabled = true;
                Courses.IsEnabled = true;
                backupNo.IsEnabled = true;
                backupButton.IsEnabled = true;


            }
        }

        private void htmlresults_Click(object sender, RoutedEventArgs e)
        {

            var eventname = Path.GetFileNameWithoutExtension(databaselocation);
            var style = "<style>footer,header{width:100%;box-shadow:rgba(0,0,0,.156863) 0 2px 5px 0,rgba(0,0,0,.117647) 0 2px 10px 0;text-align:center}*{margin:0}header{background-color:#1e88e5;top:0;padding-top:.5%;padding-bottom:.5%}body,html{margin:0;padding:0;height:100%;width:100%;font-family:Roboto,Segoe-UI,San-Francisco,sans-serif}footer{background-color:#1976d2;bottom:0}footer p{padding:10px;color:#fff}#course-info{margin-top:.5%}@media(max-width:550px){#page-title,h1{font-weight:500}main{width:90%;margin-left:5%}td,th,tr{border-bottom:1px solid #d0d0d0;padding:3%;text-align:left!important}h1{margin-top:3%}#page-title{color:#fff;margin:0;padding:0}}@media(min-width:550px){#page-title,h1{font-weight:500}main{width:90%;margin-left:5%}td,th,tr{border-bottom:1px solid #d0d0d0;padding:1%;text-align:left!important}h1{margin-top:1%}#page-title{color:#fff;margin:0;padding:0}}table{width:98%;margin-top:1%;margin-bottom:3%;border-collapse:collapse;border-spacing:0;border-radius:5px;margin-left:1%}tr{transition:background-color .5s ease}th{font-weight:700;text-align:left}tr:hover{background-color:#e8e8e8}th{border-bottom:1.25px solid #d0d0d0}</style>";
            DateTime date = DateTime.Now;
            var head = $"<!DOCTYPE HTML><html lang=\"en\"><head> <title>{eventname} - Results</title> <meta charset=\"utf - 8\"> <meta name=\"description\" content=\"Results for {eventname}\"> <meta name=\"viewport\" content=\"width=device-width, initial-scale=1,minimum-scale=1\">  {style}</head><body> <header> <h1 id=\"page-title\">{eventname} - Results</h1> </header> <main>";
            var footer = $"</table></div></main> <footer> <p>Results compiled on {date.ToString("dd/MM/yyyy")} at {date.ToString("H:mm")} using RocketDownload</p></footer></body></html>";
            var resultswrite = "";
            using (var db = new LiteDatabase(@databaselocation))
            {
                var course = "";
                var entries = db.GetCollection<Entry>("entries");
                var results = entries.Find(Query.EQ("Downloaded", true)).OrderBy(x => x.Course).ThenBy(x => x.Time);
                var resultlist = from result in results select result;
                var a = 1;
                var distance = "";
                var climb = "";
                foreach (var result in resultlist)
                {
                    if (result.Course == "") { }
                    else if (result.Course != course)
                    {
                        if (a == 1)
                        {
                            var courses = db.GetCollection<Course>("courses");

                            var results1 = courses.Find(Query.EQ("Name", result.Course));
                            var resultlist1 = from result1 in results1 select result1;

                            foreach (var result1 in resultlist1)
                            {
                                distance = result1.Distance;
                                climb = result1.Climb;
                                break;
                            }
                            resultswrite += $" <div class=\"course\"> <h1 id=\"course-title\">{result.Course}</h1> <p id=\"course-info\"> {distance}km {climb}m</p><table><tr> <th>Pos.</th> <th>Name</th> <th>Club</th> <th>Class</th> <th>Time</th> </tr> ";
                            resultswrite += $"<tr> <td> {a}</td><td> {result.Name}</td><td> {result.Club} </td><td> {result.Class} </td><td>{result.Time} </td></tr>";
                        }
                        else
                        {
                            var courses = db.GetCollection<Course>("courses");

                            var results1 = courses.Find(Query.EQ("Name", result.Course));
                            var resultlist1 = from result1 in results1 select result1;
                            if (result.Course != "Unknown") {
                                foreach (var result1 in resultlist1)
                                {
                                    distance = result1.Distance;
                                    climb = result1.Climb;
                                    break;
                                }
                                a = 1;
                                resultswrite += $" </table></div><div class=\"course\"> <h1 id=\"course-title\">{result.Course}</h1> <p id=\"course-info\"> {distance}km {climb}m</p><table><tr> <th>Pos.</th> <th>Name</th> <th>Club</th> <th>Class</th> <th>Time</th> </tr> ";
                                resultswrite += $"<tr> <td> {a}</td><td> {result.Name}</td><td> {result.Club} </td><td> {result.Class} </td><td>{result.Time} </td></tr>";


                            }
                            else
                            {
                                resultswrite += $" </table></div><div class=\"course\"> <h1 id=\"course-title\">{result.Course}</h1><table><tr> <th>Pos.</th> <th>Name</th> <th>Club</th> <th>Class</th> <th>Time</th> </tr> ";
                                resultswrite += $"<tr> <td> {a}</td><td> {result.Name}</td><td> {result.Club} </td><td> {result.Class} </td><td>{result.Time} </td></tr>";
                            }

                        }
                        course = result.Course;
                        a += 1;
                    }
                    else
                    {       
                        resultswrite += $"<tr> <td> {a}</td><td> {result.Name}</td><td> {result.Club} </td><td> {result.Class} </td><td>{result.Time} </td></tr>";
                        a += 1;
                    }
                }
            }
            var text = head + resultswrite + footer;
            File.WriteAllText(@databaselocation.Split('.')[0] + ".html", text);
            resultsLog("\nFile Written to: " + databaselocation.Split('.')[0] + ".html");




        }
        private void AddCourse_Click(object sender, RoutedEventArgs e)
        {
            var name = CourseName.Text;
            var nos = textCourseNos.Text;
            List<string> controls = new List<string>();
            int lineCount = textCourseNos.LineCount;

            for (int line = 0; line < lineCount; line++)
            {
                if (textCourseNos.GetLineText(line) != "")
                {
                    controls.Add(textCourseNos.GetLineText(line));
                }

            }

            using (var db = new LiteDatabase(@databaselocation))
            {
                var course = new Course
                {
                    Name = name,

                };
                var courses = db.GetCollection<Course>("courses");
                var results = courses.Find(Query.EQ("Name", name));
                var resultlist = from result in results select result;
                var done = 0;
                foreach (var result in resultlist)
                {
                    var course1 = new Course
                    {
                        Name = name,
                        ControlCodes = controls.ToArray(),
                        Climb = CourseClimb.Text,
                        Distance = CourseDistance.Text,
                    };

                    course1.Id = result.Id;
                    courses.Update(course1);
                    done = 1;
                    break;
                }
                if (done == 0)
                {
                    var course1 = new Course
                    {
                        Name = name,
                        ControlCodes = controls.ToArray(),
                        Climb = CourseClimb.Text,
                        Distance = CourseDistance.Text,
                    };
                    courses.Insert(course1);


                }

                CourseName.Text = "";
                CourseDistance.Text = "";
                CourseClimb.Text = "";
                textCourseNos.Text = "";
            }
        }

        private void SearchCourse_Click(object sender, RoutedEventArgs e)
        {
            CourseDistance.Text = "";
            CourseClimb.Text = "";
            textCourseNos.Text = "";
            using (var db = new LiteDatabase(@databaselocation))
            {

                var courses = db.GetCollection<Course>("courses");

                var results = courses.Find(Query.EQ("Name", CourseName.Text));
                var resultlist = from result in results select result;
                var text = "";

                foreach (var result in resultlist)
                {
                    CourseName.Text = result.Name;
                    CourseClimb.Text = result.Climb;
                    CourseDistance.Text = result.Distance;

                      
                    textCourseNos.Text = "";
                    foreach (var control in result.ControlCodes)
                    {
                        text+=control + "\n";

                    }
                    
                    textCourseNos.Text = text;
                    break;
                }
                



            }
        }
        private void AddXMLCourse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.CheckFileExists = true;
            openFileDialog.CheckPathExists = true;
            openFileDialog.Title = "Open Courses from IOF 3.0 XML";
            openFileDialog.Filter = "XML File (*.xml)|*.xml|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                using (var db = new LiteDatabase(@databaselocation))
                {
                    var courses = db.GetCollection<Course>("courses");
                    var xmlfile = openFileDialog.FileName;
                XmlDocument doc = new XmlDocument();
                doc.Load(xmlfile);

                foreach (XmlNode node in doc.DocumentElement.ChildNodes)
                {
                        if (node.Name == "RaceCourseData")
                        {
                            foreach (XmlNode node1 in node.ChildNodes)
                            {
                                if (node1.Name == "Course")
                                {
                                    var course2 = new Course { };
                                    List<string> controls = new List<string>();
                                    course2.Name = node1["Name"].InnerText;
                                    var thousand = Convert.ToInt32(node1["Length"].InnerText) / 1000;
                                    CourseDistance.Text = Convert.ToString(thousand) + "." + Convert.ToString(Convert.ToInt32(node1["Length"].InnerText) - thousand * 1000);

                                    course2.Climb = node1["Climb"].InnerText;
                                    foreach (XmlNode node2 in node1.ChildNodes)
                                    {

                                        if (node2.Name == "CourseControl")
                                        {
                                            if (node2["Control"].InnerText != "S" )
                                            {
                                                if (node2["Control"].InnerText != "F")
                                                {
                                                    controls.Add(node2["Control"].InnerText);
                                                }

                                            }

                                        }
                                    };
                                    course2.ControlCodes = controls.ToArray();
                                    courses.Insert(course2);
                                    textCourseNos.AppendText("Added Course: "+course2.Name+"\n");
                                    
                                }
                            }
                        }
                    }
                }
            }
        }

        private void writeLogColorNL(RichTextBox box, string text, string color)
        {
            listboxLog.Dispatcher.Invoke(() =>
            {
                BrushConverter bc = new BrushConverter();
                TextRange tr = new TextRange(box.Document.ContentEnd.InsertLineBreak(), box.Document.ContentEnd);

                tr.Text = text;
                try
                {
                    tr.ApplyPropertyValue(TextElement.ForegroundProperty, bc.ConvertFromString(color));
                    listboxLog.ScrollToEnd();
                }
                catch (FormatException) { }
            });
        }
        private void writeLogColor(RichTextBox box, string text, string color)
        {
            listboxLog.Dispatcher.Invoke(() =>
            {
                BrushConverter bc = new BrushConverter();
                TextRange tr = new TextRange(box.Document.ContentEnd, box.Document.ContentEnd);

                tr.Text = text;
                try
                {
                    tr.ApplyPropertyValue(TextElement.ForegroundProperty, bc.ConvertFromString(color));
                    listboxLog.ScrollToEnd();
                }
                catch (FormatException) { }
            });        }
        private void resultsLog(string text)
        {
            comboboxPortsList.Dispatcher.Invoke(() =>
            {
                textResults.AppendText(text + "\n");


            });

        }
        private void resultsClear()
        {
            comboboxPortsList.Dispatcher.Invoke(() =>
            {
                textResults.Text = "";

            });

        }

        private void backupButton_Click(object sender, RoutedEventArgs e)
        {
            if (backupNo.Text == null)
            {
                backupNo.Text = "-backup";
            }
            var newloc = databaselocation.Split('.')[0]+"-backup"+backupNo.Text+".db";
            File.Copy(databaselocation, newloc);
            backupNo.Text = "-- Backed Up --";
        }
    }
}

