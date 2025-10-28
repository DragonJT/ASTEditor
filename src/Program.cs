
using Raylib_cs;

static class RaylibHelper
{
    public static bool IsKeyPressed(KeyboardKey key)
    {
        return Raylib.IsKeyPressed(key) || Raylib.IsKeyPressedRepeat(key);
    }

    public static Rectangle Expand(this Rectangle rect, float radius)
    {
        return new Rectangle(rect.X - radius, rect.Y - radius, rect.Width + radius * 2, rect.Height + radius * 2);
    }
}

abstract class Node
{
    public string value = "";
    public Node? parent = null;
    public List<Node> children = [];

    public Node(Node? parent)
    {
        this.parent = parent;
    }

    public abstract void Draw(Window window, NodeTree nodeTree);
    public abstract void Input(Window window, NodeTree nodeTree);
}

class NodeTree : GUI
{
    Node root;
    public Node selected;
    int x;
    int y;
    int depth;

    public NodeTree(Node root) : base(true)
    {
        this.root = root;
        selected = root;
    }

    public void DrawText(string text, Color color)
    {
        Raylib.DrawText(text, x + 2, y, Program.style.fontSize, color);
        x += Raylib.MeasureText(text, Program.style.fontSize) + 4;
    }

    public void DrawSpace()
    {
        x += Program.style.spaceSize;
    }

    public void NewLine()
    {
        y += Program.style.fontSize;
        x = 0;
    }

    public void DrawChildren(Window window, List<Node> children)
    {
        depth++;
        foreach (var c in children)
        {
            x = Program.style.spaceSize * depth * 4 + Program.style.border;
            c.Draw(window, this);
        }
        depth--;
    }

    public void DrawSelected(Window window, Node node)
    {
        if (node == selected)
        {
            Raylib.DrawRectangle((int)window.rect.X, y, (int)window.rect.Width, Program.style.fontSize, Color.RayWhite);
            Raylib.DrawRectangleLines((int)window.rect.X, y, (int)window.rect.Width, Program.style.fontSize, Color.Black);
        }
    }

    public override int Height(Window window)
    {
        return (int)(window.rect.Height - window.y - Program.style.border);
    }

    public override void Update(Window window)
    {
        y = window.y;
        x = (int)(window.rect.X + Program.style.border);
        if (RaylibHelper.IsKeyPressed(KeyboardKey.Enter) && window.IsActive(this))
        {
            selected.Input(window, this);
        }
        root.Draw(window, this);
    }
}

class Module : Node
{
    public TextBox name;

    public Module(Node parent) : base(parent)
    {
        name = new TextBox();
    }

    public override void Draw(Window window, NodeTree nodeTree)
    {
        nodeTree.DrawSelected(window, this);
        nodeTree.DrawText("module", Color.SkyBlue);
        nodeTree.DrawSpace();
        nodeTree.DrawText(name.text, Color.DarkGreen);
        nodeTree.NewLine();
        nodeTree.DrawChildren(window, children);
    }

    public static void CreateModule(Window menu, Node node, NodeTree nodeTree, Window window)
    {
        menu.Add(new Button("module", () =>
        {
            var m = new Module(node);
            node.children.Add(m);
            nodeTree.selected = m;
            m.Input(window, nodeTree);

            var form = new Window(window, new Rectangle(100, 100, 400, 400));
            form.Add(m.name);
            m.name.onEnter = () =>
            {
                form.Delete();
            };
        }));
    }

    public override void Input(Window window, NodeTree nodeTree)
    {
        var menu = new Window(window, new Rectangle(100, 100, 400, 400));
        CreateModule(menu, this, nodeTree, window);
    }
}

class Root : Node
{
    public Root() : base(null) { }

    public override void Draw(Window window, NodeTree nodeTree)
    {
        nodeTree.DrawSelected(window, this);
        nodeTree.DrawText("root", Color.SkyBlue);
        nodeTree.NewLine();
        nodeTree.DrawChildren(window, children);
    }
    
    public override void Input(Window window, NodeTree nodeTree)
    {
        var menu = new Window(window, new Rectangle(100, 100, 400, 400));
        Module.CreateModule(menu, this, nodeTree, window);
    }
}

class Program
{
    public static Style style = new ();

    static void Main()
    {
        Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);
        Raylib.InitWindow(1000, 800, "ASTEditor");
        Raylib.MaximizeWindow();
        Raylib.SetExitKey(KeyboardKey.Null);

        var root = new Root();

        var form = new Window(null, new Rectangle(0, 0, 1000, 800));
        form.Add(new NodeTree(root));

        while (!Raylib.WindowShouldClose())
        {
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.White);

            form.Update();

            Raylib.EndDrawing();
        }
        Raylib.CloseWindow();
    }
}