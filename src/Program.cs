
using System.Numerics;
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

interface IDisplay
{
    void Display(NodeTree displayNodeTree, Node node);
}

class DisplayParameters(Color punctuationColor) : IDisplay
{
    public void Display(NodeTree displayNodeTree, Node node)
    {
        displayNodeTree.DrawText("(", punctuationColor);
        for (var i = 0; i < node.children.Count; i++)
        {
            displayNodeTree.Draw(node.children[i]);
            if (i < node.children.Count - 1)
            {
                displayNodeTree.DrawText(",", punctuationColor);
                displayNodeTree.DrawSpace();
            }
        }
        displayNodeTree.DrawText(")", punctuationColor);
    }
}

class DisplayChildX(int x) : IDisplay
{
    public void Display(NodeTree displayNodeTree, Node node)
    {
        displayNodeTree.Draw(node.children[x]);
    }
}

class DisplayValue(Color color) : IDisplay
{
    public void Display(NodeTree displayNodeTree, Node node)
    {
        displayNodeTree.DrawText(node.value!, color);
    }
}

class DisplayNewLine : IDisplay
{
    public void Display(NodeTree displayNodeTree, Node node)
    {
        displayNodeTree.NewLine();
    }
}

class DisplaySpace : IDisplay
{
    public void Display(NodeTree displayNodeTree, Node node)
    {
        displayNodeTree.DrawSpace();
    }
}

class DisplayType(Color color) : IDisplay
{
    public void Display(NodeTree displayNodeTree, Node node)
    {
        displayNodeTree.DrawText(node.nodeType.name, color);
    }
}

class DisplayChildren : IDisplay
{
    public void Display(NodeTree displayNodeTree, Node node)
    {
        displayNodeTree.DrawChildren(node);
    }
}

class DisplayChildrenOfChild(int childID) : IDisplay
{
    public void Display(NodeTree displayNodeTree, Node node)
    {
        displayNodeTree.DrawChildren(node.children[childID]);
    }
}

class DefaultTypes
{
    public NodeType[] types;
    public int id;

    public DefaultTypes(NodeType[] types, int id)
    {
        this.types = types;
        this.id = id;
    }
}

class NodeType
{
    public string name;
    public bool hasValue;
    public DefaultTypes? defaultTypes;
    public List<NodeType> subTypes = [];
    public List<IDisplay> display = [];

    public NodeType(string name, bool hasValue)
    {
        this.name = name;
        this.hasValue = hasValue;
    }
}

class Node
{
    public NodeType nodeType;
    public string? value;
    public Node? parent = null;
    public List<Node> children = [];

    public Node(Node? parent, NodeType nodeType, string? value)
    {
        this.parent = parent;
        this.nodeType = nodeType;
        this.value = value;
    }
}

class MenuItem
{
    public string name;
    public Action action;

    public MenuItem(string name, Action action)
    {
        this.name = name;
        this.action = action;
    }
}

class NodeTree : GUI
{
    Node root;
    int x;
    int y;
    int depth;
    int fontSize = 50;
    int spaceSize;

    public void DrawText(string text, Color color)
    {
        Raylib.DrawText(text, x+2, y, fontSize, color);
        x += Raylib.MeasureText(text, fontSize)+4;
    }

    public void DrawSpace()
    {
        x += spaceSize;
    }

    public void NewLine()
    {
        y += fontSize;
        x = 0;
    }

    public void DrawChildren(Node node)
    {
        depth++;
        foreach (var c in node.children)
        {
            x += spaceSize * 4 * depth;
            Draw(c);
        }

        depth--;
    }

    static Action CreateForm(GUI parent, Menu menu, Vector2 pos, int fontSize, Node node, NodeType nodeType)
    {
        return () =>
        {
            var rect = new Rectangle(pos.X, pos.Y, 600, fontSize);
            var form = new Form(parent, rect.Expand(50));
            var textBox = new TextBox(form, rect, fontSize);
            textBox.onEnter = () =>
            {
                var newNode = new Node(node, nodeType, textBox.text);
                if (nodeType.defaultTypes != null)
                {
                    foreach (var n in nodeType.defaultTypes.types)
                    {
                        newNode.children.Add(new Node(newNode, n, "void"));
                    }
                }
                node.children.Add(newNode);
                form.parent!.children.Remove(form);
            };
            menu.parent!.children.Remove(menu);
        };
    }

    static void CreateAddMenuItems(GUI parent, Menu menu, Vector2 pos, int fontSize, Node node)
    {
        var addNodeMenuItem = node.nodeType.subTypes
            .Select(t => new MenuItem($"Add {t.name}", CreateForm(parent, menu, pos, fontSize, node, t)))
            .ToArray();
        menu.menuItems.AddRange(addNodeMenuItem);
        
    }

    public void Draw(Node node)
    {
        var startY = y;
        foreach (var display in node.nodeType.display)
        {
            display.Display(this, node);
        }
        var m = Raylib.GetMousePosition();
        if (m.Y > startY && m.Y < startY + fontSize && Raylib.IsMouseButtonPressed(MouseButton.Left))
        {
            var menu = new Menu(parent!, m, 400, fontSize);
            if (node.nodeType.defaultTypes == null)
            {
                CreateAddMenuItems(parent!, menu, m, fontSize, node);
            }
            else if (node.nodeType.defaultTypes.id >= 0)
            {
                CreateAddMenuItems(parent!, menu, m, fontSize, node.children[node.nodeType.defaultTypes.id]);
            }
            if (node.nodeType.hasValue)
            {
                menu.menuItems.Add(new MenuItem("Rename", () =>
                {
                    var rect = new Rectangle(m.X, m.Y, 600, fontSize);
                    var form = new Form(parent, rect.Expand(50));
                    var textBox = new TextBox(form, rect, fontSize);
                    textBox.onEnter = () =>
                    {
                        node.value = textBox.text;
                        form.parent!.children.Remove(form);
                    };
                }));
            }
            if (node.parent != null)
            {
                menu.menuItems.Add(new MenuItem("Delete", () =>
                {
                    node.parent.children.Remove(node);
                    menu.parent!.children.Remove(menu);
                }));
            }
        }
    }

    public NodeTree(GUI parent, Node root)
    {
        this.parent = parent;
        parent.children.Add(this);
        this.root = root;
    }

    public override void Update()
    {
        x = 0;
        y = 0;
        depth = 0;
        spaceSize = Raylib.MeasureText(" ", fontSize);
        Draw(root);
    }
}

class Form : GUI
{
    Rectangle rect;

    public Form(GUI? parent, Rectangle rect)
    {
        this.parent = parent;
        if (parent != null)
        {
            parent.children.Add(this);
        }
        this.rect = rect;
    }

    public override void Update()
    {
        Raylib.DrawRectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height, Color.White);
        for (var i = 0; i < children.Count; i++)
        {
            children[i].Update();
        }
        Raylib.DrawRectangleLinesEx(rect, 2, Color.Black);
    }
}


class Program
{
    static void Main()
    {
        Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);
        Raylib.InitWindow(1000, 800, "ASTEditor");
        Raylib.MaximizeWindow();

        var form = new Form(null, new Rectangle(0,0,1000, 800));
        var exprType = new NodeType("expression", true);
        var typeType = new NodeType("type", true);
        var nameType = new NodeType("name", true);
        var paramsType = new NodeType("parameters", false);
        var bodyType = new NodeType("statements", false);
        var funcType = new NodeType("function", true);
        var classType = new NodeType("class", true);
        var moduleType = new NodeType("module", true);
        var rootType = new NodeType("root", false);

        rootType.subTypes.Add(moduleType);
        moduleType.subTypes.AddRange([classType, funcType]);
        classType.subTypes.AddRange([funcType]);
        bodyType.subTypes.AddRange([exprType]);

        funcType.defaultTypes = new DefaultTypes([typeType, paramsType, bodyType], 2);
        paramsType.defaultTypes = new DefaultTypes([typeType, nameType], -1);
        typeType.display.Add(new DisplayValue(Color.Blue));

        paramsType.display.Add(new DisplayParameters(Color.DarkGray));

        rootType.display.AddRange([
            new DisplayType(Color.SkyBlue),
            new DisplayNewLine(),
            new DisplayChildren()]);

        moduleType.display.AddRange([
            new DisplayType(Color.SkyBlue),
            new DisplaySpace(),
            new DisplayValue(Color.DarkGreen),
            new DisplayNewLine(),
            new DisplayChildren()]);

        classType.display.AddRange([
            new DisplayType(Color.SkyBlue),
            new DisplaySpace(),
            new DisplayValue(Color.Blue),
            new DisplayNewLine(),
            new DisplayChildren()]);

        funcType.display.AddRange([
            new DisplayChildX(0),
            new DisplaySpace(),
            new DisplayValue(Color.DarkGreen),
            new DisplayChildX(1),
            new DisplayNewLine(),
            new DisplayChildrenOfChild(2),
        ]);
        var root = new Node(null, rootType, null);
        new NodeTree(form, root);

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