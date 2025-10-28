
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
    public int selectedID = 0;
    public bool startFrame = true;
    public Window? parent;
    public Window? child;
    public Rectangle rect;
    List<GUI> guis = [];
    List<GUI> selectableGUIs = [];
    public int y;

    public Window(Window? parent, Rectangle rect)
    {
        SetParent(parent);
        this.rect = rect;
    }

    public void Add(GUI gui)
    {
        guis.Add(gui);
        if (gui.isSelectable)
        {
            selectableGUIs.Add(gui);
        }
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
            if (selectableGUIs.Count > 0)
            {
                if (RaylibHelper.IsKeyPressed(KeyboardKey.Up))
                {
                    selectedID--;
                    if (selectedID < 0) selectedID = selectableGUIs.Count - 1;
                }
                if (RaylibHelper.IsKeyPressed(KeyboardKey.Down))
                {
                    selectedID++;
                    if (selectedID > selectableGUIs.Count - 1) selectedID = 0;
                }
            }
        }
        float height = style.border + style.spacing;
        foreach (var g in guis)
        {
            height += g.Height(this);
        }
        height += style.border;

        if(rect.Height < height)
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
        child?.Update();
        startFrame = false;
    }

    public void SetParent(Window? parent)
    {
        if (parent != null)
        {
            this.parent = parent;
            parent.child = this;
        }
    }

    public void Delete()
    {
        if (parent != null) parent.child = null;
    }

    public bool Active => child == null && !startFrame;

    public bool IsActive(GUI gui)
    {
        if (Active)
        {
            var index = selectableGUIs.IndexOf(gui);
            return index == selectedID;
        }
        return false;
    }
}

abstract class GUI(bool isSelectable)
{
    public bool isSelectable = isSelectable;

    public abstract void Update(Window window);
    public abstract int Height(Window window);
}

class TextBox : GUI
{
    public string text = "";
    public Action? onEnter;

    public TextBox() : base(true) { }

    public override int Height(Window window) => Program.style.fontSize + Program.style.spacing;

    public override void Update(Window window)
    {
        var style = Program.style;
        Color border = Color.Black;
        if (window.IsActive(this))
        {
            int key = Raylib.GetCharPressed();
            while (key > 0)
            {
                if ((key >= 32) && (key <= 125))
                {
                    text += (char)key;
                }

                key = Raylib.GetCharPressed();
            }

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
        var r = window.rect;
        var rect = new Rectangle(
            r.X + r.Width * style.labelFraction,
            window.y,
            r.Width * (1 - style.labelFraction) - style.border,
            style.fontSize);
        Raylib.DrawRectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height, Color.RayWhite);
        Raylib.DrawRectangleLinesEx(rect, 2, border);
        Raylib.DrawText(text, (int)rect.X, (int)rect.Y, style.fontSize, Color.Black);
    }
}

class Label : GUI
{
    string label;

    public Label(string label) : base(false)
    {
        this.label = label;
    }

    public override int Height(Window window) => 0;

    public override void Update(Window window)
    {
        var style = Program.style;
        Raylib.DrawText(label, style.border, window.y, style.fontSize, style.labelColor);
    }
}

class Header : GUI
{
    string header;

    public Header(string header) : base(false)
    {
        this.header = header;
    }

    public override int Height(Window window) => Program.style.headerFontSize + Program.style.spacing;

    public override void Update(Window window)
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

class Button : GUI
{
    string text;
    Action action;

    public Button(string text, Action action) : base(true)
    {
        this.text = text;
        this.action = action;
    }

    public override int Height(Window window) => Program.style.fontSize + Program.style.spacing;

    public override void Update(Window window)
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

    public BoolBox(bool value) : base(true)
    {
        this.value = value;
    }

    public override int Height(Window window) => Program.style.fontSize + Program.style.spacing;

    public override void Update(Window window)
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