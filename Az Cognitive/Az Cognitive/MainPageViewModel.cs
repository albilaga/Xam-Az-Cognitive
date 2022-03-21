using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Acr.UserDialogs;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using MvvmHelpers;
using Newtonsoft.Json;
using Xamarin.Essentials;
using Xamarin.Forms;

// ReSharper disable AsyncVoidLambda

namespace Az_Cognitive
{
    public class MainPageViewModel : BaseViewModel
    {
        private FileResult _photo;

        private ImageSource _image;

        public ImageSource Image
        {
            get => _image;
            private set => SetProperty(ref _image, value);
        }

        private ICommand _pickPhotoCommand;

        public ICommand PickPhotoCommand => _pickPhotoCommand ??= new Command(async () =>
        {
            try
            {
                _photo = await MediaPicker.PickPhotoAsync().ConfigureAwait(false);
                var stream = await _photo.OpenReadAsync();
                Image = ImageSource.FromStream(() => stream);
                ImageDescription = "";
            }
            catch (FeatureNotSupportedException)
            {
                // Feature is not supported on the device
            }
            catch (PermissionException)
            {
                // Permissions not granted
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CapturePhotoAsync THREW: {ex.Message}");
            }
        });

        private ICommand _analysePhotoCommand;

        public ICommand AnalysePhotoCommand => _analysePhotoCommand ??= new Command(async () =>
        {
            if (_photo is null || Image is null)
            {
                return;
            }

            try
            {
                UserDialogs.Instance.ShowLoading("Evaluating....");
                const string subscriptionKey = "<YOUR_API_KEY>";
                const string endpoint = "<YOUR ENDPOINT>";
                var client = new ComputerVisionClient(new ApiKeyServiceClientCredentials(subscriptionKey))
                    { Endpoint = endpoint };

                var features = new List<VisualFeatureTypes?>
                {
                    VisualFeatureTypes.Categories, VisualFeatureTypes.Description,
                    VisualFeatureTypes.Faces, VisualFeatureTypes.ImageType,
                    VisualFeatureTypes.Tags, VisualFeatureTypes.Adult,
                    VisualFeatureTypes.Color, VisualFeatureTypes.Brands,
                    VisualFeatureTypes.Objects,
                };
                var stream = await _photo.OpenReadAsync().ConfigureAwait(false);
                var result = await client.AnalyzeImageInStreamAsync(stream, features).ConfigureAwait(false);
                var json = JsonConvert.SerializeObject(result);
                // var sb = new StringBuilder();
                // sb.AppendLine("Summary:");
                // foreach (var caption in results.Description.Captions)
                // {
                //     sb.AppendLine($"{caption.Text} with confidence {caption.Confidence}");
                // }
                //
                // ImageDescription = sb.ToString();
                ImageDescription =
                    result.Categories.Any(x => x.Name.StartsWith("food", StringComparison.OrdinalIgnoreCase))
                        ? "FOOD"
                        : "NOT FOOD";
            }
            catch (Exception)
            {
                UserDialogs.Instance.HideLoading();
                UserDialogs.Instance.Alert("There is something wrong. Please try again later.");
            }
            finally
            {
                UserDialogs.Instance.HideLoading();
            }
        });

        private string _imageDescription;

        public string ImageDescription
        {
            get => _imageDescription;
            private set => SetProperty(ref _imageDescription, value);
        }
    }
}