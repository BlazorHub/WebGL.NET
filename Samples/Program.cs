﻿using WebAssembly;

namespace Samples
{
    class Program
    {
        const int CanvasWidth = 320;
        const int CanvasHeight = 240;

        static void Main(string[] args)
        {
            var samples = new ISample[] 
            { 
                //new Triangle(), 
                //new RotatingCube(),
                new Draw2DImage()
            };

            foreach (var item in samples)
            {
                var canvas = CreateCanvasFor(item.GetType().Name);
                item.Run(canvas, CanvasWidth, CanvasHeight);
            }
        }

        static JSObject CreateCanvasFor(string sampleName)
        {
            var document = Runtime.GetGlobalObject("document") as JSObject;
            var canvas = (JSObject)document.Invoke("createElement", "canvas");
            canvas.SetObjectProperty("width", CanvasWidth);
            canvas.SetObjectProperty("height", CanvasHeight);
            var body = (JSObject)document.GetObjectProperty("body");
            var header = (JSObject)document.Invoke("createElement", "h1");
            var headerText = (JSObject)document.Invoke("createTextNode", sampleName);
            header.Invoke("appendChild", headerText);
            body.Invoke("appendChild", header);
            body.Invoke("appendChild", canvas);

            return canvas;
        }
    }
}
