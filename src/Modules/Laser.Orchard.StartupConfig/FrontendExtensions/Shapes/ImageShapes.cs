using Orchard;
using Orchard.ContentManagement;
using Orchard.DisplayManagement;
using Orchard.Environment;
using Orchard.Forms.Services;
using Orchard.Logging;
using Orchard.MediaLibrary.Models;
using Orchard.MediaProcessing.Models;
using Orchard.MediaProcessing.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Web.Mvc;

namespace Laser.Orchard.StartupConfig.FrontendExtensions.Shapes {
    public class ImageShapes : IDependency {
        private readonly Work<IImageProfileManager> _imageProfileManager;

        public ImageShapes(
            Work<IImageProfileManager> imageProfileManager) {

            _imageProfileManager = imageProfileManager;
        }

        public ILogger Logger { get; set; }

        /// <summary>
        /// This method allows the configuration of a img html tag. It can use a MediaPart, an ImagePart
        /// or the path to an image file. It can resize the image. It can also be configured to set up
        /// attributes for lazy load of the image: the thumbnail can be configured in many ways.
        /// </summary>
        /// <param name="Shape"></param>
        /// <param name="Display"></param>
        /// <param name="Output"></param>
        /// <param name="MediaPart">The MediaPart to be used to create an image tag.</param>
        /// <param name="ImagePart">The ImagePart to be used to create an image tag.</param>
        /// <param name="ImagePath">The path to the source image for the image tag.</param>
        /// <param name="Width">The width we will attempt using for the image profile used in the tag.</param>
        /// <param name="Height">The height we will attempt using for the image profile used in the tag.</param>
        /// <param name="Mode">Resize mode for the image.</param>
        /// <param name="Alignment">Alignment for the resized image.</param>
        /// <param name="PadColor">Color to use when padding the image.</param>
        /// <param name="alt">This parameter will be used for the alt attribute of the image tag in case
        /// the MediaPart's is empty, or there is no MediaPart.</param>
        /// <param name="title">This parameter will be used for the title attribute of the image tag in case
        /// the MediaPart's is empty, or there is no MediaPart.</param>
        /// <param name="StubMediaPart">The MediaPart to be used for the thumbnail of the image tag.</param>
        /// <param name="StubImagePart">The ImagePart to be used for the thumbnail of the image tag.</param>
        /// <param name="StubImagePath">The path to the source image for the thumbnail of the image tag.</param>
        /// <param name="StubWidth">The width we will attempt using for the image profile used in the tag.</param>
        /// <param name="StubHeight">The height we will attempt using for the image profile used in the tag.</param>
        /// <param name="StubMode">Resize mode for the image.</param>
        /// <param name="StubAlignment">Alignment for the resized image.</param>
        /// <param name="StubPadColor">Color to use when padding the image.</param>
        /// <param name="htmlAttributes">Additional html attributes for the img tag. This parameter is used, for example
        /// to set the css class for the image.</param>
        /// <remarks>The source image is chosen between a ContentItem or a path to an image. In the latter
        /// case, the Width and Height parameters must both be greater than 0. It is possible to use the main
        /// image as a source for the stub: to do this, don't set StubMediaPart, StubImagePart and StubImagePath,
        /// but provide positive values for StubWidth and StubHeight.</remarks>
        [Shape]
        public void ImageTag(
            dynamic Shape,
            dynamic Display,
            TextWriter Output,
            /* Parameters for the image we want to show */
            MediaPart MediaPart,
            ImagePart ImagePart,
            string ImagePath,
            int Width,
            int Height,
            string Mode,
            string Alignment,
            string PadColor,
            string alt,
            string title,
            /* Parameters for the thumbnail used for lazy loading of image */
            MediaPart StubMediaPart,
            ImagePart StubImagePart,
            string StubImagePath,
            int StubWidth,
            int StubHeight,
            string StubMode,
            string StubAlignment,
            string StubPadColor,
            IDictionary<string, object> htmlAttributes) {

            ImageInfo mainImage = ImageInfo.New(MediaPart, ImagePart, Width, Height);
            ImageInfo mainImage15 = ImageInfo.New(MediaPart, ImagePart, (int)Math.Round(Width / 1.5), (int)Math.Round(Height / 1.5));
            ImageInfo mainImage2 = ImageInfo.New(MediaPart, ImagePart, Width / 2, Height / 2);
            ImageInfo mainImage3 = ImageInfo.New(MediaPart, ImagePart, Width / 3, Height / 3);
            ImageInfo mainImage4 = ImageInfo.New(MediaPart, ImagePart, Width / 4, Height / 4);
            if (mainImage == null) {
                // attempt to validate the information without using the ContentItem
                mainImage = ImageInfo.New(ImagePath, Width, Height);
            }
            if (mainImage15 == null) {
                // attempt to validate the information without using the ContentItem
                mainImage15 = ImageInfo.New(ImagePath, (int)Math.Round(Width / 1.5), (int)Math.Round(Height / 1.5));
            }
            if (mainImage2 == null) {
                // attempt to validate the information without using the ContentItem
                mainImage2 = ImageInfo.New(ImagePath, Width/2, Height/2);
            }
            if (mainImage3 == null) {
                // attempt to validate the information without using the ContentItem
                mainImage3 = ImageInfo.New(ImagePath, Width / 3, Height / 3);
            }
            if (mainImage4 == null) {
                // attempt to validate the information without using the ContentItem
                mainImage4 = ImageInfo.New(ImagePath, Width/4, Height/4);
            }

            if (mainImage == null) {
                // attempt to validate the information without using the ContentItem
                mainImage = ImageInfo.New(ImagePath, Width, Height);
            }

            if (mainImage == null) {
                Logger.Error("It was impossible to figure out the image to render.");
                return;
            }

            // At this point, mainImage contains all the information to render the image
            if (MediaPart != null) {
                // The MediaPart may have its own alternate text and title
                title = string.IsNullOrWhiteSpace(MediaPart.Title) ? title : MediaPart.Title;
                alt = string.IsNullOrWhiteSpace(MediaPart.AlternateText) ? alt : MediaPart.AlternateText;
            }

            // Check whether we are also trying to render a thumbnail/stub for lazy loading
            // thumbnail cannot be larger than full image:
            StubWidth = StubWidth > mainImage.Width ? mainImage.Width : StubWidth;
            // thumbnail cannot be taller than full image:
            StubHeight = StubHeight > mainImage.Height ? mainImage.Height : StubHeight;
            ImageInfo stubImage = ImageInfo.New(StubMediaPart, StubImagePart, StubWidth, StubHeight);
            if (stubImage == null) {
                stubImage = ImageInfo.New(StubImagePath, StubWidth, StubHeight);
            }
            // Lastly, we may want to generate a stub based on the main image:
            if (stubImage == null && StubWidth > 0 && StubHeight > 0) {
                stubImage = new ImageInfo() {
                    ContentItem = mainImage.ContentItem,
                    ImagePath = mainImage.ImagePath,
                    Width = StubWidth,
                    Height = StubHeight
                };
            }

            // Generate tag, with attributes that are common to both normal and lazyload cases
            TagBuilder tagBuilder = new TagBuilder("img");
            tagBuilder.MergeAttributes(htmlAttributes); // this for example allows adding a class
            if (!string.IsNullOrWhiteSpace(title)) {
                tagBuilder.MergeAttribute("title", title);
            }
            if (!string.IsNullOrWhiteSpace(alt)) {
                tagBuilder.MergeAttribute("alt", alt);
            }

           

            // Generate stuff needed for the image profiles
            var imagePath = GetProfileUrl(mainImage, Mode, Alignment, PadColor);
            var imagePath15 = GetProfileUrl(mainImage15, Mode, Alignment, PadColor);
            var imagePath2 = GetProfileUrl(mainImage2, Mode, Alignment, PadColor);
            var imagePath3 = GetProfileUrl(mainImage3, Mode, Alignment, PadColor);
            var imagePath4 = GetProfileUrl(mainImage4, Mode, Alignment, PadColor);

            tagBuilder.MergeAttribute("srcset", imagePath4 + " " + Width / 4 + "w, " + imagePath3 + " " + Width / 3 + "w, " + imagePath2 + " " + Width / 2 + "w, " + imagePath15 + " " + (int)Math.Round(Width/1.5) + "w," + imagePath + " " + Width+"w");
            //tagBuilder.MergeAttribute("sizes", "(min-width:" + Width + "px) " + Width + "px,(min-width:" + (int)Math.Round(Width / 1.5) + "px) " + (int)Math.Round(Width / 1.5) + "px,(min-width: " + Width / 2 + "px) " + Width / 2 + "px,(min-width: " + Width / 3 + "px) " + Width / 3 + "px,(min-width: " + Width / 4 + "px) " + Width / 4 + "px");
            tagBuilder.MergeAttribute("sizes", "50vw");
            if (stubImage == null) {
                // "normal" behaviour, without stub image for lazyload
                tagBuilder.MergeAttribute("src", imagePath4);
            } else {
                // stub settings:
                StubMode = string.IsNullOrWhiteSpace(StubMode) ? Mode : StubMode;
                StubAlignment = string.IsNullOrWhiteSpace(StubAlignment) ? Alignment : StubAlignment;
                StubPadColor = string.IsNullOrWhiteSpace(StubPadColor) ? PadColor : StubPadColor;
                // generate tag with stub image for lazyload
                var imageStub = GetProfileUrl(stubImage, StubMode, StubAlignment, StubPadColor);
                tagBuilder.MergeAttribute("src", imageStub);
                tagBuilder.MergeAttribute("data-src", imagePath4);
            }

            Output.Write(tagBuilder.ToString(TagRenderMode.Normal));
        }

        private string GetProfileUrl(
            MediaPart mediaPart,
            int Width,
            int Height,
            string Mode,
            string Alignment,
            string PadColor) {

            return GetProfileUrl(mediaPart.MediaUrl, Width, Height, Mode, Alignment, PadColor, mediaPart.ContentItem);
        }

        private string GetProfileUrl(
            string Path,
            int Width,
            int Height,
            string Mode,
            string Alignment,
            string PadColor,
            ContentItem contentItem = null) {

            var filter = Filter(Width, Height, Mode, Alignment, PadColor);

            var profile = Profile(Width, Height, Mode, Alignment, PadColor);

            return GetProfileUrl(Path, profile, filter, contentItem);
        }

        private string GetProfileUrl(
            ImageInfo imageInfo,
            string Mode,
            string Alignment,
            string PadColor) {

            if (imageInfo.ContentItem == null) {
                return imageInfo.ImagePath;
            }

            var filter = Filter(imageInfo.Width, imageInfo.Height, Mode, Alignment, PadColor);
            var profile = Profile(imageInfo.Width, imageInfo.Height, Mode, Alignment, PadColor);

            return GetProfileUrl(imageInfo.ImagePath, profile, filter, imageInfo.ContentItem);
        }

        private string GetProfileUrl(string Path, string Profile, FilterRecord CustomFilter, ContentItem ContentItem) {
            return _imageProfileManager.Value.GetImageProfileUrl(Path, Profile, CustomFilter, ContentItem);
        }

        private string Profile(
            int Width,
            int Height,
            string Mode,
            string Alignment,
            string PadColor) {
            return "Transform_Resize"
                + "_w_" + Convert.ToString(Width)
                + "_h_" + Convert.ToString(Height)
                + "_m_" + Convert.ToString(Mode)
                + "_a_" + Convert.ToString(Alignment)
                + "_c_" + Convert.ToString(PadColor);
        }

        private FilterRecord Filter(
            int Width,
            int Height,
            string Mode,
            string Alignment,
            string PadColor) {

            var state = new Dictionary<string, string> {
                {"Width", Width.ToString(CultureInfo.InvariantCulture)},
                {"Height", Height.ToString(CultureInfo.InvariantCulture)},
                {"Mode", Mode},
                {"Alignment", Alignment},
                {"PadColor", PadColor},
            };

            return new FilterRecord {
                Category = "Transform",
                Type = "Resize",
                State = FormParametersHelper.ToString(state)
            };
        }

        /// <summary>
        /// Class used in the methods here to represent an Image to be rendered
        /// </summary>
        class ImageInfo {
            public ContentItem ContentItem { get; set; }
            public string ImagePath { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }

            public static ImageInfo New(
                MediaPart mediaPart,
                ImagePart imagePart,
                int width,
                int height) {


                if (mediaPart == null) {
                    mediaPart = imagePart.As<MediaPart>();
                } else if (imagePart == null) {
                    imagePart = mediaPart.As<ImagePart>();
                }

                // at minimum, we need a MediaPart to get to a file
                if (mediaPart == null) {
                    return null;
                }

                if (imagePart != null) {
                    // Manage Width <= 0, Height <= 0
                    if (width <= 0) {
                        width = imagePart.Width;
                    }
                    if (height <= 0) {
                        height = imagePart.Height;
                    }
                }

                var imagePath = mediaPart.MediaUrl;

                var img = New(imagePath, width, height);
                if (img != null) {
                    img.ContentItem = mediaPart.ContentItem;
                }
                return img;
            }

            public static ImageInfo New(
                string imagePath,
                int width,
                int height) {

                // validate information
                if (string.IsNullOrWhiteSpace(imagePath)) {
                    // since this is for external path, we won't be able to resize it, so it makes
                    // no sense to strictly require width and height
                    return null;
                }

                var img = new ImageInfo() {
                    ContentItem = null,
                    ImagePath = imagePath,
                    Width = width,
                    Height = height
                };
                return img;
            }
        }

    }
}