
using System.Data;
using Raylib_cs;

class Style
{
    public int fontSize = 50;
    public int headerFontSize = 80;
    public Color headerColor = Color.DarkGray;
    public int spacing = 5;
    public int border = 20;
    public float labelFraction = 0.4f;
    public Color labelColor = Color.Black;
    public int spaceSize = 20;
}

class Window
{
    Window? parent;
    Window? child;
    public Rectangle rect;
    public GUI? selected;
    public bool firstFrame = true;
    GUI[] guis;
    GUI[] selectableGUIs;
    public int y;
    public Action? AdditionalInput;

    public void AttachChild(Window child)
    {
        this.child = child;
        child.parent = this;
    }

    void SetDefaultSelected()
    {
        if (selectableGUIs.Length > 0)
            selected = selectableGUIs[0];
        else
            selected = null;
    }

    public void UpdateGUIs(GUI[] guis)
    {
        this.guis = guis;
        selectableGUIs = [.. guis.SelectMany(g => g.Selectables)];
        if (!selectableGUIs.Contains(selected))
        {
            SetDefaultSelected();
        }
    }

    public Window(Window? parent, Rectangle rect, GUI[] guis)
    {
        parent?.AttachChild(this);
        this.rect = rect;
        this.guis = guis;
        selectableGUIs = [.. guis.SelectMany(g => g.Selectables)];
        SetDefaultSelected();
    }

    public void MoveSelected(int delta)
    {
        var index = Array.IndexOf(selectableGUIs, selected);
        index += delta;
        if (index < 0) index = selectableGUIs.Length - 1;
        if (index > selectableGUIs.Length - 1) index = 0;
        selected = selectableGUIs[index];
    }

    public void Update()
    {
        var style = Program.style;
        y = (int)(rect.Y + style.border + style.spacing);
        var active = Active;
        if (active)
        {
            if (Raylib.IsKeyPressed(KeyboardKey.Escape))
            {
                Delete();
            }
            if (selectableGUIs.Length > 0)
            {
                if (RaylibHelper.IsKeyPressed(KeyboardKey.Up))
                {
                    MoveSelected(-1);
                }
                if (RaylibHelper.IsKeyPressed(KeyboardKey.Down))
                {
                    MoveSelected(1);
                }
            }
            AdditionalInput?.Invoke();
        }
        float height = style.border + style.spacing;
        foreach (var g in guis)
        {
            height += g.Height(this);
        }
        height += style.border;

        if (rect.Height < height)
        {
            rect.Height = height;
        }
        Raylib.DrawRectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height, Color.White);
        foreach (var g in guis)
        {
            g.Update(this);
            y += g.Height(this);
        }
        Raylib.DrawRectangleLinesEx(rect, 2, Color.Black);
        firstFrame = false;
        child?.Update();
    }

    public bool Active => child == null && !firstFrame;

    public bool IsActive(GUI gui)
    {
        if (Active)
        {
            return selected == gui;
        }
        return false;
    }

    public void Delete()
    {
        if (parent != null)
            parent.child = null;
    }
}

interface GUI
{
    GUI[] Selectables { get; }
    void Update(Window window);
    int Height(Window window);
}

class TextBox : GUI
{
    public string text = "";
    public Action? onEnter;

    public TextBox() { }

    public GUI[] Selectables => [this];

    public int Height(Window window) => Program.style.fontSize + Program.style.spacing;

    public void Update(Window window)
    {
        var style = Program.style;
        Color border = Color.Black;
        if (window.IsActive(this))
        {
            text += RaylibHelper.GetText();
            if (RaylibHelper.IsKeyPressed(KeyboardKey.Backspace) && text.Length > 0)
            {
                text = text[..^1];
            }
            if (RaylibHelper.IsKeyPressed(KeyboardKey.Enter))
            {
                onEnter?.Invoke();
            }
            border = Color.Red;
        }
        var r = new Rectangle(
            window.rect.X + window.rect.Width * style.labelFraction,
            window.y,
            window.rect.Width * (1 - style.labelFraction) - style.border,
            style.fontSize);
        Raylib.DrawRectangle((int)r.X, (int)r.Y, (int)r.Width, (int)r.Height, Color.RayWhite);
        Raylib.DrawRectangleLinesEx(r, 2, border);
        Raylib.DrawText(text, (int)r.X, (int)r.Y, style.fontSize, Color.Black);
    }
}

class Label : GUI
{
    string label;
    GUI value;

    public Label(string label, GUI value)
    {
        this.label = label;
        this.value = value;
    }

    public GUI[] Selectables => [value];

    public int Height(Window window) => value.Height(window);

    public void Update(Window window)
    {
        var style = Program.style;
        Raylib.DrawText(label, style.border, window.y, style.fontSize, style.labelColor);
        value.Update(window);
    }
}

class Header(string header) : GUI
{
    string header = header;

    public GUI[] Selectables => throw new NotImplementedException();

    public int Height(Window window) => Program.style.headerFontSize + Program.style.spacing;

    public void Update(Window window)
    {
        var style = Program.style;
        var length = Raylib.MeasureText(header, style.headerFontSize);
        Raylib.DrawText(
            header,
            (int)(window.rect.Width / 2 + window.rect.X - length / 2f),
            (int)window.y,
            style.headerFontSize,
            style.headerColor);
    }
}

class SearchBox : GUI
{
    string text;
    List<Button> searches = [];
    Window searchWindow;
    bool attachChild = true;
    bool dirty = false;
    public Action<string>? onNumberInput;

    public void Add(string search, Action action)
    {
        searches.Add(new Button(search, action));
        dirty = true;
    }

    public SearchBox(string text)
    {
        this.text = text;
        searchWindow = new Window(null, new Rectangle(), []);
        searchWindow.AdditionalInput = OnInput;
    }

    public GUI[] Selectables => [this];

    public int Height(Window window) => Program.style.fontSize + Program.style.spacing;

    bool IsNumberInput()
    {
        return onNumberInput != null && text.Length > 0 && (char.IsDigit(text[0]) || text[0] == '.');
    }

    void UpdateCurrentSearches()
    {
        if (IsNumberInput())
        {
            searchWindow.UpdateGUIs([]);
        }
        else
        {
            var currentSearches = searches.Where(s => s.text.StartsWith(text)).ToArray();
            while (currentSearches.Length == 0 && text.Length > 0)
            {
                text = text[..^1];
                currentSearches = [.. searches.Where(s => s.text.StartsWith(text))];
            }
            searchWindow.UpdateGUIs(currentSearches);
        }
    }

    public void OnInput()
    {
        if (Raylib.IsKeyPressed(KeyboardKey.Enter) && IsNumberInput())
        {
            onNumberInput!(text);
        }
        var startText = text;
        text += RaylibHelper.GetText();
        if (RaylibHelper.IsKeyPressed(KeyboardKey.Backspace) && text.Length > 0)
        {
            text = text[..^1];
        }
        if (startText != text)
        {
            attachChild = true;
            UpdateCurrentSearches();
        }
    }

    public void Update(Window window)
    {
        if (dirty)
        {
            UpdateCurrentSearches();
            dirty = false;
        }
        if (window.Active)
        {
            OnInput();
        }
        if (attachChild)
        {
            window.AttachChild(searchWindow);
            attachChild = false;
        }
        
        var style = Program.style;
        Color border = window.selected == this ? Color.Red : Color.Black;
        var r = window.rect;
        var rect = new Rectangle(r.X + style.border, window.y, r.Width - style.border * 2, style.fontSize);
        Raylib.DrawRectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height, Color.RayWhite);
        Raylib.DrawRectangleLinesEx(rect, 2, border);
        Raylib.DrawText(text, (int)rect.X, (int)rect.Y, style.fontSize, Color.Black);

        var searchRect = new Rectangle(r.X + r.Width * style.labelFraction, window.y + style.fontSize, 400, 0);
        searchWindow.rect = searchRect;
        
    }
}

class Button : GUI
{
    public string text;
    Action action;

    public Button(string text, Action action)
    {
        this.text = text;
        this.action = action;
    }

    public GUI[] Selectables => [this];

    public int Height(Window window) => Program.style.fontSize + Program.style.spacing;

    public void Update(Window window)
    {
        var style = Program.style;
        var rect = new Rectangle(window.rect.X + style.border, window.y, window.rect.Width - style.border * 2, style.fontSize);
        var active = window.IsActive(this);
        if (active)
        {
            Raylib.DrawRectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height, Color.Red);
        }
        Raylib.DrawText(text, (int)rect.X, (int)rect.Y, style.fontSize, Color.Black);
        if (RaylibHelper.IsKeyPressed(KeyboardKey.Enter) && active)
        {
            action();
        }
    }
}

class BoolBox : GUI
{
    bool value;

    public BoolBox(bool value)
    {
        this.value = value;
    }

    public GUI[] Selectables => [this];

    public int Height(Window window) => Program.style.fontSize + Program.style.spacing;

    public void Update(Window window)
    {
        var style = Program.style;
        var active = window.IsActive(this);
        Color color = Color.Black;
        if (active)
            color = Color.Red;

        var rect = new Rectangle(window.rect.X + window.rect.Width * style.labelFraction, window.y, style.fontSize, style.fontSize);
        if (value)
            Raylib.DrawRectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height, color);
        else
            Raylib.DrawRectangleLinesEx(rect, 4, color);

        if (RaylibHelper.IsKeyPressed(KeyboardKey.Enter) && active)
            value = !value;
    }
}