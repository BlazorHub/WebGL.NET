﻿using System;
using System.Linq;
using WebAssembly;
using WebAssembly.Core;

namespace WebGLDotNET
{
    public abstract class JSHandler : IDisposable
    {
        internal JSObject Handle { get; set; }

        public bool IsDisposed { get; private set; }

        ~JSHandler()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (IsDisposed)
            {
                return;
            }

            IsDisposed = true;

            Handle.Dispose();
        }
    }

    public partial class WebGLActiveInfo : JSHandler
    {
    }

    public partial class WebGLContextAttributes : JSHandler
    {
    }

    public partial class WebGLObject : JSHandler
    {
    }

    public partial class WebGLRenderingContext : WebGLRenderingContextBase
    {
        public WebGLRenderingContext(JSObject canvas) 
            : base(canvas, "webgl")
        {
        }

        public WebGLRenderingContext(JSObject canvas, JSObject contextAttributes) 
            : base(canvas, "webgl", contextAttributes)
        {
        }
    }

    public abstract partial class WebGLRenderingContextBase : JSHandler
    {
        protected readonly JSObject gl;

        public WebGLRenderingContextBase(JSObject canvas, string contextType) : this(canvas, contextType, null)
        {
        }

        public WebGLRenderingContextBase(JSObject canvas, string contextType, JSObject contextAttributes)
        {
            if (!IsSupported)
            {
                throw new PlatformNotSupportedException(
                    $"The context '{contextType}' is not supported in this browser");
            }

            gl = (JSObject)canvas.Invoke("getContext", contextType, contextAttributes);
        }

        public static bool IsSupported => CheckWindowPropertyExists("WebGLRenderingContext");

        public ITypedArray CastNativeArray(object managedArray)
        {
            var arrayType = managedArray.GetType();
            ITypedArray array;

            // Here are listed some JavaScript array types:
            // https://github.com/mono/mono/blob/a7f5952c69ae76015ccaefd4dfa8be2274498a21/sdks/wasm/bindings-test.cs
            if (arrayType == typeof(byte[]))
            {
                array = Uint8Array.From((byte[])managedArray);
            }
            else if (arrayType == typeof(float[]))
            {
                array = Float32Array.From((float[])managedArray);
            }
            else if (arrayType == typeof(ushort[]))
            {
                array = Uint16Array.From((ushort[])managedArray);
            }
            else
            {
                throw new NotImplementedException();
            }

            return array;
        }

        protected static bool CheckWindowPropertyExists(string property)
        {
            var window = (JSObject)Runtime.GetGlobalObject();
            var exists = window.GetObjectProperty(property) != null;

            return exists;
        }

        private void DisposeArrayTypes(object[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];

                if (arg is ITypedArray typedArray && typedArray != null)
                {
                    var disposable = (IDisposable)typedArray;
                    disposable.Dispose();
                }
                if (arg is WebAssembly.Core.Array jsArray && jsArray != null)
                {
                    var disposable = (IDisposable)jsArray;
                    disposable.Dispose();

                }
            }
        }

        protected object Invoke(string method, params object[] args)
        {
            var actualArgs = Translate(args);
            var result = gl.Invoke(method, actualArgs);
            DisposeArrayTypes(actualArgs);

            return result;
        }

        protected T Invoke<T>(string method, params object[] args)
            where T : JSHandler, new()
        {
            var actualArgs = Translate(args);
            var rawResult = gl.Invoke(method, actualArgs);
            DisposeArrayTypes(actualArgs);

            var result = new T();
            result.Handle = (JSObject)rawResult;

            return result;
        }

        protected T[] InvokeForArray<T>(string method, params object[] args) =>
            ((object[])gl.Invoke(method, args))
                .Cast<T>()
                .ToArray();

        protected T InvokeForBasicType<T>(string method, params object[] args)
            where T : IConvertible
        {
            var result = Invoke(method, args);

            return (T)result;
        }

        private object[] Translate(object[] args)
        {
            var actualArgs = new object[args.Length];

            for (int i = 0; i < actualArgs.Length; i++)
            {
                var arg = args[i];

                if (arg == null)
                {
                    actualArgs[i] = null;
                    continue;
                }

                if (arg is JSHandler jsHandler)
                {
                    arg = jsHandler.Handle;
                }
                else if (arg is System.Array array)
                {
                    if (((System.Array)arg).GetType().GetElementType().IsPrimitive)
                        arg = CastNativeArray(array);
                    else
                    {
                        // WebAssembly.Core.Array or Runtime should probably provide some type of
                        // helper functions for doing this.  I will put it on my todo list.
                        var argArray = new WebAssembly.Core.Array();
                        foreach(var item in (System.Array)arg)
                        {
                            argArray.Push(item);
                        }
                        arg = argArray;
                    }
                }

                actualArgs[i] = arg;
            }

            return actualArgs;
        }
    }

    public partial class WebGLShaderPrecisionFormat : JSHandler
    {
    }

    public partial class WebGLUniformLocation : JSHandler, IDisposable
    {
    }

    public partial class WebGL2RenderingContext : WebGL2RenderingContextBase
    {
        public WebGL2RenderingContext(JSObject canvas) 
            : base(canvas, "webgl2")
        { 
        }

        public WebGL2RenderingContext(JSObject canvas, JSObject contextAttributes) 
            : base(canvas, "webgl2", contextAttributes)
        {
        }
    }

    public abstract partial class WebGL2RenderingContextBase : WebGLRenderingContextBase
    {
        public WebGL2RenderingContextBase(JSObject canvas, string contextType) 
            : base(canvas, contextType)
        {
        }

        public WebGL2RenderingContextBase(JSObject canvas, string contextType, JSObject contextAttributes) 
            : base(canvas, contextType, contextAttributes)
        {
        }

        new public static bool IsSupported => CheckWindowPropertyExists("WebGL2RenderingContext");
    }
}
