using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;
using Noor.Services;

namespace Noor.Views;

public sealed partial class HijriCalendarPage : Page
{
    private int _currentHijriYear;
    private int _currentHijriMonth;
    private readonly ObservableCollection<ReminderItem> _reminders = new();

    private static readonly string[] ArabicDayNames = { "الأحد", "الإثنين", "الثلاثاء", "الأربعاء", "الخميس", "الجمعة", "السبت" };
    private static readonly string[] EnglishDayNames = { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };

    private int _converterUpdating;

    public HijriCalendarPage()
    {
        this.InitializeComponent();
        var today = DateTime.Today;
        _currentHijriYear = HijriDateService.GetHijriYear(today);
        _currentHijriMonth = HijriDateService.GetHijriMonth(today);
        InitConverterCombos(today);
        ReminderCategoryCombo.SelectedIndex = 0;
        ReminderNotifyCombo.SelectedIndex = 0;
        ReminderRepeatCombo.SelectedIndex = 0;
        LoadReminders();
        BuildCalendar();
        UpdateTodayInfo();
        UpdateConverterResult(today);
    }

    // ═══════ NAVIGATION ═══════

    private void OnBackClick(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack) Frame.GoBack();
    }

    private void OnPrevMonthClick(object sender, RoutedEventArgs e)
    {
        _currentHijriMonth--;
        if (_currentHijriMonth < 1) { _currentHijriMonth = 12; _currentHijriYear--; }
        BuildCalendar();
        AnimateCalendar();
    }

    private void OnNextMonthClick(object sender, RoutedEventArgs e)
    {
        _currentHijriMonth++;
        if (_currentHijriMonth > 12) { _currentHijriMonth = 1; _currentHijriYear++; }
        BuildCalendar();
        AnimateCalendar();
    }

    private void OnTodayClick(object sender, RoutedEventArgs e)
    {
        var today = DateTime.Today;
        _currentHijriYear = HijriDateService.GetHijriYear(today);
        _currentHijriMonth = HijriDateService.GetHijriMonth(today);
        BuildCalendar();
        UpdateTodayInfo();
        AnimateCalendar();
    }

    // ═══════ TODAY INFO ═══════

    private void UpdateTodayInfo()
    {
        var today = DateTime.Today;
        var hijriDay = HijriDateService.GetHijriDay(today);
        var hijriMonth = HijriDateService.GetHijriMonth(today);
        var hijriYear = HijriDateService.GetHijriYear(today);
        var dayOfWeek = (int)today.DayOfWeek;

        TodayDayNumber.Text = hijriDay.ToString();
        TodayDayName.Text = ArabicDayNames[dayOfWeek];
        TodayGregorianText.Text = today.ToString("dddd, MMMM d, yyyy");
        TodayHijriFullText.Text = $"{hijriDay} {HijriDateService.GetHijriMonthNameEnglish(hijriMonth)} {hijriYear} AH";

        // Days until next Islamic event
        var nextEvent = GetNextIslamicEvent(today, hijriYear, hijriMonth);
        if (nextEvent != null)
        {
            var daysUntil = (nextEvent.Value - today).Days;
            TodayCountdownText.Text = daysUntil == 0
                ? $"Today: {GetEventName(hijriMonth, hijriDay)}"
                : $"{daysUntil} day{(daysUntil > 1 ? "s" : "")} to {nextEvent.Value.ToString("MMM d")}";
        }
        else
        {
            TodayCountdownText.Text = "";
        }
    }

    private DateTime? GetNextIslamicEvent(DateTime from, int hijriYear, int hijriMonth)
    {
        for (int m = 0; m <= 12; m++)
        {
            int checkMonth = ((hijriMonth - 1 + m) % 12) + 1;
            int checkYear = hijriYear + ((hijriMonth - 1 + m) / 12);
            var events = HijriDateService.GetIslamicEvents(checkMonth);
            foreach (var evt in events.OrderBy(e => e.Key))
            {
                if (checkMonth == hijriMonth && evt.Key <= HijriDateService.GetHijriDay(from) && m == 0) continue;
                var evtDate = HijriDateService.HijriToGregorian(checkYear, checkMonth, evt.Key);
                if (evtDate >= from) return evtDate;
            }
        }
        return null;
    }

    private string GetEventName(int month, int day)
    {
        var events = HijriDateService.GetIslamicEvents(month);
        return events.TryGetValue(day, out var name) ? name : "";
    }

    // ═══════ CALENDAR ═══════

    private void BuildCalendar()
    {
        var monthArabic = HijriDateService.GetHijriMonthNameArabic(_currentHijriMonth);
        var monthEnglish = HijriDateService.GetHijriMonthNameEnglish(_currentHijriMonth);

        MonthNameArabicText.Text = monthArabic;
        MonthNameEnglishText.Text = monthEnglish;
        HijriYearText.Text = _currentHijriYear.ToString();

        var daysInMonth = HijriDateService.GetDaysInHijriMonth(_currentHijriYear, _currentHijriMonth);
        var firstDayOfWeek = HijriDateService.GetFirstDayOfWeek(_currentHijriYear, _currentHijriMonth);

        var today = DateTime.Today;
        var todayHY = HijriDateService.GetHijriYear(today);
        var todayHM = HijriDateService.GetHijriMonth(today);
        var todayHD = HijriDateService.GetHijriDay(today);
        bool isCurrentMonth = _currentHijriYear == todayHY && _currentHijriMonth == todayHM;

        var events = HijriDateService.GetIslamicEvents(_currentHijriMonth);

        // Stats
        StatDaysText.Text = daysInMonth.ToString();
        StatEventsText.Text = events.Count.ToString();
        StatRemindersText.Text = _reminders.Count(r => r.HijriMonth == _currentHijriMonth && r.HijriYear == _currentHijriYear).ToString();

        // Populate reminder day combo
        ReminderDayCombo.Items.Clear();
        for (int d = 1; d <= daysInMonth; d++)
        {
            var evtName = events.ContainsKey(d) ? $" - {events[d]}" : "";
            ReminderDayCombo.Items.Add(new ComboBoxItem { Content = $"Day {d}{evtName}", Tag = d });
        }
        if (ReminderDayCombo.Items.Count > 0)
            ReminderDayCombo.SelectedIndex = 0;

        var primary = (Brush)Application.Current.Resources["PrimaryBrush"];
        var primarySurface = (Brush)Application.Current.Resources["PrimarySurfaceBrush"];
        var primaryText = (Brush)Application.Current.Resources["PrimaryTextBrush"];
        var secondaryText = (Brush)Application.Current.Resources["SecondaryTextBrush"];
        var tertiaryText = (Brush)Application.Current.Resources["TertiaryTextBrush"];
        var divider = (Brush)Application.Current.Resources["DividerBrush"];

        CalendarGrid.Children.Clear();
        CalendarGrid.RowDefinitions.Clear();
        CalendarGrid.ColumnDefinitions.Clear();

        for (int c = 0; c < 7; c++)
            CalendarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        int totalCells = firstDayOfWeek + daysInMonth;
        int totalRows = (int)Math.Ceiling(totalCells / 7.0);

        for (int row = 0; row < totalRows; row++)
        {
            CalendarGrid.RowDefinitions.Add(new RowDefinition { MinHeight = 58 });

            for (int col = 0; col < 7; col++)
            {
                int cellIndex = row * 7 + col;
                int hijriDay = cellIndex - firstDayOfWeek + 1;

                var cell = new Grid
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch
                };

                if (hijriDay >= 1 && hijriDay <= daysInMonth)
                {
                    bool isToday = isCurrentMonth && hijriDay == todayHD;
                    bool hasEvent = events.ContainsKey(hijriDay);
                    bool isFriday = col == 5;
                    bool hasReminder = _reminders.Any(r =>
                        r.HijriYear == _currentHijriYear &&
                        r.HijriMonth == _currentHijriMonth &&
                        r.HijriDay == hijriDay);

                    var gregorianDate = HijriDateService.HijriToGregorian(
                        _currentHijriYear, _currentHijriMonth, hijriDay);

                    if (isToday)
                    {
                        // TODAY — filled circle
                        var circle = new Ellipse
                        {
                            Width = 40,
                            Height = 40,
                            Fill = primary,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        };
                        cell.Children.Add(circle);

                        cell.Children.Add(new TextBlock
                        {
                            Text = hijriDay.ToString(),
                            FontSize = 16,
                            FontWeight = FontWeights.Bold,
                            Foreground = new SolidColorBrush(Colors.White),
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        });
                    }
                    else
                    {
                        var stack = new StackPanel
                        {
                            Spacing = 1,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        };

                        stack.Children.Add(new TextBlock
                        {
                            Text = hijriDay.ToString(),
                            FontSize = 15,
                            FontWeight = FontWeights.Medium,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Foreground = isFriday ? primary : primaryText
                        });

                        stack.Children.Add(new TextBlock
                        {
                            Text = gregorianDate.Day.ToString(),
                            FontSize = 9,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Foreground = tertiaryText
                        });

                        // Indicator dots
                        var dotsRow = new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Spacing = 3,
                            Margin = new Thickness(0, 2, 0, 0)
                        };

                        if (hasEvent)
                        {
                            dotsRow.Children.Add(new Ellipse
                            {
                                Width = 4,
                                Height = 4,
                                Fill = primary,
                                HorizontalAlignment = HorizontalAlignment.Center
                            });
                        }

                        if (hasReminder)
                        {
                            dotsRow.Children.Add(new Ellipse
                            {
                                Width = 4,
                                Height = 4,
                                Fill = (Brush)Application.Current.Resources["ErrorBrush"],
                                HorizontalAlignment = HorizontalAlignment.Center
                            });
                        }

                        if (dotsRow.Children.Count > 0)
                            stack.Children.Add(dotsRow);

                        cell.Children.Add(stack);

                        if (hasEvent)
                        {
                            cell.Background = primarySurface;
                            cell.CornerRadius = new CornerRadius(8);
                        }
                    }
                }

                Grid.SetRow(cell, row);
                Grid.SetColumn(cell, col);
                CalendarGrid.Children.Add(cell);
            }
        }

        // Events
        EventsList.Children.Clear();
        if (events.Count > 0)
        {
            EventsCard.Visibility = Visibility.Visible;
            var sorted = events.OrderBy(e => e.Key).ToList();

            for (int i = 0; i < sorted.Count; i++)
            {
                var (day, name) = sorted[i];
                var evtDate = HijriDateService.HijriToGregorian(_currentHijriYear, _currentHijriMonth, day);

                var evtRow = new Grid();
                evtRow.ColumnDefinitions.Add(new ColumnDefinition { Width = 4 });
                evtRow.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });

                var bar = new Border { Background = primary, CornerRadius = new CornerRadius(2, 0, 0, 2) };
                Grid.SetColumn(bar, 0);

                var content = new StackPanel { Spacing = 2, Margin = new Thickness(16, 12, 20, 12) };

                content.Children.Add(new TextBlock
                {
                    Text = name,
                    FontSize = 14,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = primaryText
                });

                var details = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
                details.Children.Add(new TextBlock
                {
                    Text = $"{day} {HijriDateService.GetHijriMonthNameEnglish(_currentHijriMonth)} {_currentHijriYear}",
                    FontSize = 12,
                    Foreground = secondaryText,
                    VerticalAlignment = VerticalAlignment.Center
                });
                details.Children.Add(new TextBlock
                {
                    Text = "•",
                    FontSize = 12,
                    Foreground = tertiaryText,
                    VerticalAlignment = VerticalAlignment.Center
                });
                details.Children.Add(new TextBlock
                {
                    Text = evtDate.ToString("ddd, MMM d, yyyy"),
                    FontSize = 12,
                    Foreground = tertiaryText,
                    VerticalAlignment = VerticalAlignment.Center
                });
                content.Children.Add(details);

                Grid.SetColumn(content, 1);
                evtRow.Children.Add(bar);
                evtRow.Children.Add(content);
                EventsList.Children.Add(evtRow);

                if (i < sorted.Count - 1)
                    EventsList.Children.Add(new Border { Height = 1, Background = divider, Margin = new Thickness(20, 0, 20, 0) });
            }
        }
        else
        {
            EventsCard.Visibility = Visibility.Collapsed;
        }

        BuildRemindersList();
    }

    // ═══════ DATE CONVERTER ═══════

    private void InitConverterCombos(DateTime today)
    {
        _converterUpdating++;

        var monthNames = new[] { "", "January", "February", "March", "April", "May", "June",
                                 "July", "August", "September", "October", "November", "December" };

        ConverterYearCombo.Items.Clear();
        for (int y = 1900; y <= 2077; y++)
            ConverterYearCombo.Items.Add(new ComboBoxItem { Content = y.ToString(), Tag = y });

        ConverterMonthCombo.Items.Clear();
        for (int m = 1; m <= 12; m++)
            ConverterMonthCombo.Items.Add(new ComboBoxItem { Content = monthNames[m], Tag = m });

        var yearIdx = today.Year - 1900;
        var monthIdx = today.Month - 1;
        if (yearIdx >= 0 && yearIdx < ConverterYearCombo.Items.Count)
            ConverterYearCombo.SelectedIndex = yearIdx;
        if (monthIdx >= 0 && monthIdx < ConverterMonthCombo.Items.Count)
            ConverterMonthCombo.SelectedIndex = monthIdx;

        PopulateConverterDays(today.Year, today.Month, today.Day);

        _converterUpdating--;
    }

    private void PopulateConverterDays(int year, int month, int selectDay)
    {
        _converterUpdating++;
        var prevCount = ConverterDayCombo.Items.Count;
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var shouldSelect = selectDay > 0 && selectDay <= daysInMonth;

        if (prevCount != daysInMonth)
        {
            ConverterDayCombo.Items.Clear();
            for (int d = 1; d <= daysInMonth; d++)
                ConverterDayCombo.Items.Add(new ComboBoxItem { Content = d.ToString(), Tag = d });
        }

        if (shouldSelect)
            ConverterDayCombo.SelectedIndex = selectDay - 1;
        else if (ConverterDayCombo.Items.Count > 0)
            ConverterDayCombo.SelectedIndex = 0;

        _converterUpdating--;
    }

    private void OnConverterDateChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_converterUpdating > 0) return;
        if (ConverterYearCombo.SelectedItem is not ComboBoxItem yItem || yItem.Tag is not int year) return;
        if (ConverterMonthCombo.SelectedItem is not ComboBoxItem mItem || mItem.Tag is not int month) return;

        var prevDay = ConverterDayCombo.SelectedItem is ComboBoxItem dItem && dItem.Tag is int pd ? pd : 1;
        PopulateConverterDays(year, month, prevDay);

        if (ConverterDayCombo.SelectedItem is not ComboBoxItem selItem || selItem.Tag is not int day) return;

        var date = new DateTime(year, month, day);
        UpdateConverterResult(date);
    }

    private void OnTodayConverterClick(object sender, RoutedEventArgs e)
    {
        var today = DateTime.Today;
        _converterUpdating++;
        var yearIdx = today.Year - 1900;
        var monthIdx = today.Month - 1;
        if (yearIdx >= 0 && yearIdx < ConverterYearCombo.Items.Count)
            ConverterYearCombo.SelectedIndex = yearIdx;
        if (monthIdx >= 0 && monthIdx < ConverterMonthCombo.Items.Count)
            ConverterMonthCombo.SelectedIndex = monthIdx;
        PopulateConverterDays(today.Year, today.Month, today.Day);
        _converterUpdating--;
        UpdateConverterResult(today);
    }

    private void UpdateConverterResult(DateTime date)
    {
        GregorianDayNameText.Text = $"{EnglishDayNames[(int)date.DayOfWeek]}, {date:MMMM d, yyyy}";

        if (date.Year < 1900 || date.Year > 2077)
        {
            ConverterResultText.Text = "Date must be between 1900\u20132077";
            ConverterResultArabicText.Text = "";
            ConverterDayNameText.Text = "";
            return;
        }

        try
        {
            var hYear = HijriDateService.GetHijriYear(date);
            var hMonth = HijriDateService.GetHijriMonth(date);
            var hDay = HijriDateService.GetHijriDay(date);
            var monthEnglish = HijriDateService.GetHijriMonthNameEnglish(hMonth);
            var monthArabic = HijriDateService.GetHijriMonthNameArabic(hMonth);
            var arabicDayName = ArabicDayNames[(int)date.DayOfWeek];

            ConverterResultText.Text = $"{hDay} {monthEnglish} {hYear} AH";
            ConverterResultArabicText.Text = $"{monthArabic} {hDay}, {hYear}";
            ConverterDayNameText.Text = $"{arabicDayName} \u2022 {hDay} {monthEnglish}";

            AnimateConverterResult();
        }
        catch
        {
            ConverterResultText.Text = "Date out of range";
            ConverterResultArabicText.Text = "";
            ConverterDayNameText.Text = "";
        }
    }

    private void AnimateConverterResult()
    {
        ConverterResultPanel.Opacity = 0;
        ConverterResultPanel.Translation = new System.Numerics.Vector3(0, 8, 0);
        var anim = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = new Duration(TimeSpan.FromMilliseconds(250))
        };
        anim.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
        var sb = new Storyboard();
        Storyboard.SetTarget(anim, ConverterResultPanel);
        Storyboard.SetTargetProperty(anim, "Opacity");
        sb.Children.Add(anim);
        sb.Begin();
    }

    // ═══════ REMINDERS ═══════

    private void OnAddReminderToggleClick(object sender, RoutedEventArgs e)
    {
        var isVisible = AddReminderForm.Visibility == Visibility.Visible;
        AddReminderForm.Visibility = isVisible ? Visibility.Collapsed : Visibility.Visible;
        AddReminderIcon.Glyph = isVisible ? "\uE710" : "\uE711";
        AddReminderText.Text = isVisible ? "Add" : "Close";
    }

    private void OnCancelReminderClick(object sender, RoutedEventArgs e)
    {
        AddReminderForm.Visibility = Visibility.Collapsed;
        AddReminderIcon.Glyph = "\uE710";
        AddReminderText.Text = "Add";
        ReminderNameBox.Text = "";
        ReminderCategoryCombo.SelectedIndex = 0;
        ReminderNotifyCombo.SelectedIndex = 0;
        ReminderRepeatCombo.SelectedIndex = 0;
    }

    private void OnSaveReminderClick(object sender, RoutedEventArgs e)
    {
        if (ReminderDayCombo.SelectedIndex < 0 || ReminderDayCombo.SelectedItem is not ComboBoxItem dayItem || dayItem.Tag is not int selectedDay)
            return;

        var category = "Religious";
        if (ReminderCategoryCombo.SelectedItem is ComboBoxItem catItem && catItem.Tag is string catTag && !string.IsNullOrEmpty(catTag))
            category = catTag;

        var repeatStr = "None";
        if (ReminderRepeatCombo.SelectedItem is ComboBoxItem repItem && repItem.Tag is string repTag && !string.IsNullOrEmpty(repTag))
            repeatStr = repTag;

        var notifyStr = "0";
        if (ReminderNotifyCombo.SelectedItem is ComboBoxItem notifItem && notifItem.Tag is string notifTag && !string.IsNullOrEmpty(notifTag))
            notifyStr = notifTag;

        var reminder = new ReminderItem
        {
            HijriYear = _currentHijriYear,
            HijriMonth = _currentHijriMonth,
            HijriDay = selectedDay,
            Name = string.IsNullOrWhiteSpace(ReminderNameBox.Text)
                ? $"Day {selectedDay}"
                : ReminderNameBox.Text,
            Category = category,
            Repeat = repeatStr,
            NotifyDaysBefore = int.Parse(notifyStr),
            CreatedDate = DateTime.Today
        };

        _reminders.Add(reminder);
        SaveReminders();
        BuildCalendar();

        AddReminderForm.Visibility = Visibility.Collapsed;
        AddReminderIcon.Glyph = "\uE710";
        AddReminderText.Text = "Add";
        ReminderNameBox.Text = "";
        ReminderCategoryCombo.SelectedIndex = 0;
        ReminderNotifyCombo.SelectedIndex = 0;
        ReminderRepeatCombo.SelectedIndex = 0;
    }

    private void BuildRemindersList()
    {
        RemindersList.Children.Clear();
        var monthReminders = _reminders
            .Where(r => r.HijriYear == _currentHijriYear && r.HijriMonth == _currentHijriMonth)
            .OrderBy(r => r.HijriDay)
            .ToList();

        if (monthReminders.Count == 0)
        {
            RemindersEmpty.Visibility = Visibility.Visible;
            return;
        }

        RemindersEmpty.Visibility = Visibility.Collapsed;
        var primary = (Brush)Application.Current.Resources["PrimaryBrush"];
        var primaryText = (Brush)Application.Current.Resources["PrimaryTextBrush"];
        var secondaryText = (Brush)Application.Current.Resources["SecondaryTextBrush"];
        var tertiaryText = (Brush)Application.Current.Resources["TertiaryTextBrush"];
        var divider = (Brush)Application.Current.Resources["DividerBrush"];
        var warning = (Brush)Application.Current.Resources["WarningBrush"];

        for (int i = 0; i < monthReminders.Count; i++)
        {
            var r = monthReminders[i];
            var gregDate = HijriDateService.HijriToGregorian(r.HijriYear, r.HijriMonth, r.HijriDay);

            Brush catColor = r.Category switch
            {
                "Personal" => warning,
                "Family" => (Brush)Application.Current.Resources["MaghribBrush"],
                "Study" => (Brush)Application.Current.Resources["FajrBrush"],
                _ => primary
            };

            string catLabel = r.Category switch
            {
                "Personal" => "\U0001F4E6 Personal",
                "Family" => "\U0001F3E0 Family",
                "Study" => "\U0001F4DA Study",
                _ => "\U0001F54C Religious"
            };

            string repeatStr = r.Repeat switch
            {
                "Yearly" => "\U0001F501 Every year",
                "Monthly" => "\U0001F501 Every month",
                _ => ""
            };

            var row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var bar = new Border { Background = catColor, CornerRadius = new CornerRadius(2, 0, 0, 2) };
            Grid.SetColumn(bar, 0);

            var content = new StackPanel { Spacing = 3, Margin = new Thickness(16, 10, 8, 10) };

            var topRow = new Grid();
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            topRow.Children.Add(new TextBlock
            {
                Text = r.Name ?? $"Day {r.HijriDay}",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = primaryText,
                VerticalAlignment = VerticalAlignment.Center
            });

            var catBadge = new Border
            {
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(8, 3, 8, 3),
                Background = catColor,
                VerticalAlignment = VerticalAlignment.Center
            };
            catBadge.Child = new TextBlock
            {
                Text = catLabel,
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Colors.White)
            };
            Grid.SetColumn(catBadge, 1);
            topRow.Children.Add(catBadge);
            content.Children.Add(topRow);

            content.Children.Add(new TextBlock
            {
                Text = $"{r.HijriDay} {HijriDateService.GetHijriMonthNameEnglish(r.HijriMonth)} {r.HijriYear} \u2022 {gregDate:MMM d, yyyy}",
                FontSize = 12,
                Foreground = secondaryText
            });

            if (!string.IsNullOrEmpty(repeatStr))
            {
                content.Children.Add(new TextBlock
                {
                    Text = repeatStr,
                    FontSize = 11,
                    Foreground = primary,
                    Margin = new Thickness(0, 2, 0, 0)
                });
            }

            Grid.SetColumn(content, 1);

            var deleteBtn = new Button
            {
                Background = new SolidColorBrush(Colors.Transparent),
                BorderThickness = new Thickness(0),
                Width = 32,
                Height = 32,
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(0),
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 8, 8, 0),
                Content = new FontIcon { Glyph = "\uE74D", FontSize = 14, Foreground = tertiaryText }
            };
            var reminderToRemove = r;
            deleteBtn.Click += (_, _) =>
            {
                _reminders.Remove(reminderToRemove);
                SaveReminders();
                BuildCalendar();
            };
            Grid.SetColumn(deleteBtn, 2);

            row.Children.Add(bar);
            row.Children.Add(content);
            row.Children.Add(deleteBtn);
            RemindersList.Children.Add(row);

            if (i < monthReminders.Count - 1)
                RemindersList.Children.Add(new Border { Height = 1, Background = divider, Margin = new Thickness(20, 0, 20, 0) });
        }
    }

    private void SaveReminders()
    {
        var path = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Noor", "reminders.json");
        var dir = System.IO.Path.GetDirectoryName(path)!;
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        var json = System.Text.Json.JsonSerializer.Serialize(_reminders);
        File.WriteAllText(path, json);
    }

    private void LoadReminders()
    {
        try
        {
            var path = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Noor", "reminders.json");
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var items = System.Text.Json.JsonSerializer.Deserialize<List<ReminderItem>>(json);
                if (items != null)
                    foreach (var item in items)
                        _reminders.Add(item);
            }
        }
        catch { }
    }

    // ═══════ ANIMATION ═══════

    private void AnimateCalendar()
    {
        CalendarGrid.Opacity = 0;
        var anim = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = new Duration(TimeSpan.FromMilliseconds(200))
        };
        anim.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
        var sb = new Storyboard();
        Storyboard.SetTarget(anim, CalendarGrid);
        Storyboard.SetTargetProperty(anim, "Opacity");
        sb.Children.Add(anim);
        sb.Begin();
    }
}

public class ReminderItem
{
    public int HijriYear { get; set; }
    public int HijriMonth { get; set; }
    public int HijriDay { get; set; }
    public string? Name { get; set; }
    public string Category { get; set; } = "Religious";
    public string Repeat { get; set; } = "None";
    public int NotifyDaysBefore { get; set; } = 0;
    public DateTime CreatedDate { get; set; } = DateTime.Today;
}
