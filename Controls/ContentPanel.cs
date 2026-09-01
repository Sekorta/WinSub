using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SubsonicPlayer
{
    public class ContentPanel : Panel
    {
        private Dictionary<string, Control> _pages = new Dictionary<string, Control>();
        private string _currentPage;
        private Stack<string> _history = new Stack<string>();
        private Panel _pageContainer;

        public Stack<string> History { get { return _history; } }

        public ContentPanel()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.FromArgb(30, 30, 30);

            _pageContainer = new Panel();
            _pageContainer.Dock = DockStyle.Fill;
            _pageContainer.BackColor = Color.FromArgb(30, 30, 30);

            this.Controls.Add(_pageContainer);
        }

        public void RegisterPage(string name, Control page)
        {
            page.Dock = DockStyle.Fill;
            page.Visible = false;
            _pageContainer.Controls.Add(page);
            _pages[name] = page;
        }

        public void ShowPage(string name)
        {
            if (!_pages.ContainsKey(name)) return;

            foreach (var kvp in _pages)
                kvp.Value.Visible = false;

            _pages[name].Visible = true;
            _currentPage = name;
        }

        public string CurrentPage { get { return _currentPage; } }
    }
}
