using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;

namespace WPF.MedicalImaging
{
    public enum MedicalViewerActionType
    {
        None = 0,
        //
        //定位
        //
        Positioning = 1,
        //
        // 摘要:
        //     Window leveling. Moving the mouse horizontally controls the window center parameter
        //     of the window level. Moving the mouse vertically controls the window width parameter
        //     of the window level.
        WindowLevel = 2,
        //
        // 摘要:
        //     Scaling. Moving the mouse vertically controls the scale factor of the image or
        //     the 3D object.
        Scale = 3,
        //
        // 摘要:
        //     Offset. Moving the mouse to any direction causes the image or the 3D object to
        //     move towards that direction (Zoom in or out the 3D object if you are using the
        //     Medical3DControl).
        Offset = 4,
        //
        // 摘要:
        //     Stacking. Moving the mouse vertically scrolls through the frames.
        Stack = 5,

        /// <summary>
        /// 标注笔刷 Paint, Eraser
        /// </summary>
        PaintEffect = 10,

        /// <summary>
        /// 多层画笔
        /// </summary>
        MultiplePaintEffect = 20,

        /// <summary>
        /// 球形画笔
        /// </summary>
        SpherePaintEffect = 30,

        /// <summary>
        /// 标注橡皮擦
        /// </summary>
        ScissorsEffect = 40,
    }


    public enum MedicalViewerMouseButtons
    {
        //
        // 摘要:
        //     未曾按下鼠标按钮。
        None = 0,

        //
        // 摘要:
        //     鼠标左按钮曾按下。
        Left = 1048576,
        //
        // 摘要:
        //     鼠标右按钮曾按下。
        Right = 2097152,
        //
        // 摘要:
        //     鼠标中按钮曾按下。
        Middle = 4194304,
        //
        // 摘要:
        //     第 1 个 XButton 曾按下。
        XButton1 = 8388608,
        //
        // 摘要:
        //     第 2 个 XButton 曾按下。
        XButton2 = 16777216,

        //
        // 摘要:
        //     鼠标滚轮滚动。
        Wheel = 33554432
    }

    // 鼠标状态类型
    public enum MouseStateType
    {
        MouseDown,
        MouseMove,
        MouseUp,
        MouseLevel,
        MouseWheel
    }

    internal class MouseSuperEventArgs
    {
        public MouseSuperEventArgs(MouseEventArgs args, MouseStateType mouseState)
        {
            this.InnerMouseEventArgs = args;
            this.MouseStateType = mouseState;
        }

        public MouseSuperEventArgs(MouseButtonEventArgs args, MouseStateType mouseState)
        {
            this.InnerMouseEventArgs = args;
            this.MouseStateType = mouseState;
        }

        public MouseSuperEventArgs(MouseWheelEventArgs args, MouseStateType mouseState)
        {
            this.InnerMouseEventArgs = args;
            this.MouseStateType = mouseState;
        }

        public MouseEventArgs InnerMouseEventArgs { get; private set; }

        public MouseStateType MouseStateType { get; private set; }

        public int Delta
        {
            get
            {
                if (this.InnerMouseEventArgs is MouseWheelEventArgs args)
                    return args.Delta;
                return 0;
            }
        }

        public Point GetPosition(IInputElement relativeTo)
        {
            return InnerMouseEventArgs.GetPosition(relativeTo);
        }
    }
}
