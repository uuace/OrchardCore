using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.DisplayManagement.Layout;
using OrchardCore.DisplayManagement.Shapes;
using OrchardCore.DisplayManagement.Title;

namespace OrchardCore.DisplayManagement.Razor
{
    public abstract class RazorPage<TModel> : Microsoft.AspNetCore.Mvc.Razor.RazorPage<TModel>
    {
        private dynamic _displayHelper;
        private IShapeFactory _shapeFactory;
        private OrchardRazorHelper _orchardHelper;

        private void EnsureDisplayHelper()
        {
            if (_displayHelper == null)
            {
                IDisplayHelperFactory _factory = Context.RequestServices.GetService<IDisplayHelperFactory>();
                _displayHelper = _factory.CreateHelper(ViewContext);
            }
        }

        private void EnsureShapeFactory()
        {
            if (_shapeFactory == null)
            {
                _shapeFactory = Context.RequestServices.GetService<IShapeFactory>();
            }
        }

        private void EnsureOrchardHelper()
        {
            if (_orchardHelper == null)
            {
                EnsureDisplayHelper();
                _orchardHelper = new OrchardRazorHelper(Context, _displayHelper);
            }
        }

        /// <summary>
        /// Gets a dynamic shape factory to create new shapes.
        /// </summary>
        /// <example>
        /// Usage:
        /// <code>
        /// await New.MyShape()
        /// await New.MyShape(A: 1, B: "Some text")
        /// (await New.MyShape()).A(1).B("Some text")
        /// </code>
        /// </example>
        public dynamic New
        {
            get
            {
                return Factory;
            }
        }

        /// <summary>
        /// Gets an <see cref="IShapeFactory"/> to create new shapes.
        /// </summary>
        public IShapeFactory Factory
        {
            get
            {
                EnsureShapeFactory();
                return _shapeFactory;
            }
        }

        /// <summary>
        /// Renders a shape.
        /// </summary>
        /// <param name="shape">The shape.</param>
        public Task<IHtmlContent> DisplayAsync(dynamic shape)
        {
            EnsureDisplayHelper();
            return (Task<IHtmlContent>)_displayHelper(shape);
        }

        public OrchardRazorHelper OrchardCore
        {
            get
            {
                EnsureOrchardHelper();
                return _orchardHelper;
            }
        }

        private dynamic _themeLayout;
        public dynamic ThemeLayout
        {
            get
            {
                if (_themeLayout == null)
                {
                    var layoutAccessor = Context.RequestServices.GetService<ILayoutAccessor>();

                    if (layoutAccessor == null)
                    {
                        throw new InvalidOperationException("Could not find a valid layout accessor");
                    }

                    _themeLayout = layoutAccessor.GetLayoutAsync().GetAwaiter().GetResult();
                }

                return _themeLayout;
            }

            set
            {
                _themeLayout = value;
            }
        }

        private IPageTitleBuilder _pageTitleBuilder;
        public IPageTitleBuilder Title
        {
            get
            {
                if (_pageTitleBuilder == null)
                {
                    _pageTitleBuilder = Context.RequestServices.GetRequiredService<IPageTitleBuilder>();
                }

                return _pageTitleBuilder;
            }
        }

        private IViewLocalizer _t;

        /// <summary>
        /// The <see cref="IViewLocalizer"/> instance for the current view.
        /// </summary>
        public IViewLocalizer T
        {
            get
            {
                if (_t == null)
                {
                    _t = Context.RequestServices.GetRequiredService<IViewLocalizer>();
                    ((IViewContextAware)_t).Contextualize(this.ViewContext);
                }

                return _t;
            }
        }

        /// <summary>
        /// Adds a segment to the title and returns all segments.
        /// </summary>
        /// <param name="segment">The segment to add to the title.</param>
        /// <param name="position">Optional. The position of the segment in the title.</param>
        /// <param name="separator">The html string that should separate all segments.</param>
        /// <returns>And <see cref="IHtmlContent"/> instance representing the full title.</returns>
        public IHtmlContent RenderTitleSegments(IHtmlContent segment, string position = "0", IHtmlContent separator = null)
        {
            Title.AddSegment(segment, position);
            return Title.GenerateTitle(separator);
        }

        /// <summary>
        /// Adds a segment to the title and returns all segments.
        /// </summary>
        /// <param name="segment">The segment to add to the title.</param>
        /// <param name="position">Optional. The position of the segment in the title.</param>
        /// <param name="separator">The html string that should separate all segments.</param>
        /// <returns>And <see cref="IHtmlContent"/> instance representing the full title.</returns>
        public IHtmlContent RenderTitleSegments(string segment, string position = "0", IHtmlContent separator = null)
        {
            Title.AddSegment(new HtmlString(HtmlEncoder.Encode(segment)), position);
            return Title.GenerateTitle(separator);
        }
        
        /// <summary>
        /// Renders the content zone of the layout.
        /// </summary>
        public IHtmlContent RenderLayoutBody()
        {
            var result = base.RenderBody();
            return result;
        }

        /// <summary>
        /// Creates a <see cref="TagBuilder"/> to render a shape.
        /// </summary>
        /// <param name="shape">The shape.</param>
        /// <returns>A new <see cref="TagBuilder"/>.</returns>
        public TagBuilder Tag(dynamic shape)
        {
            return Shape.GetTagBuilder(shape);
        }

        public TagBuilder Tag(dynamic shape, string tag)
        {
            return Shape.GetTagBuilder(shape, tag);
        }

        public Task<IHtmlContent> RenderBodyAsync()
        {
            return DisplayAsync(ThemeLayout.Content);
        }

        /// <summary>
        /// Renders a zone from the layout.
        /// </summary>
        /// <param name="name">The name of the zone to render.</param>
        /// <param name="required">Whether the zone is required or not.</param>
        public new Task<IHtmlContent> RenderSectionAsync(string name, bool required)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            var zone = ThemeLayout[name];

            if (required && zone != null && zone.Items.Count == 0)
            {
                throw new InvalidOperationException("Zone not found: " + name);
            }

            return DisplayAsync(zone);
        }

        public object OrDefault(object text, object other)
        {
            if (text == null || Convert.ToString(text) == "")
            {
                return other;
            }

            return text;
        }

        /// <summary>
        /// Returns the full path of the current request.
        /// </summary>
        public string FullRequestPath => Context.Request.PathBase + Context.Request.Path + Context.Request.QueryString;
    }

    public abstract class RazorPage : RazorPage<dynamic>
    {
    }
}
