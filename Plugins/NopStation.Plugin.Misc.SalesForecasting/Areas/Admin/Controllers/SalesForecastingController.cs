using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.EMMA;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Services.Catalog;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Security;
using NopStation.Plugin.Misc.Core.Controllers;
using NopStation.Plugin.Misc.Core.Filters;
using NopStation.Plugin.Misc.SalesForecasting.Areas.Admin.Factories;
using NopStation.Plugin.Misc.SalesForecasting.Areas.Admin.Models;
using NopStation.Plugin.Misc.SalesForecasting.Extensions;
using NopStation.Plugin.Misc.SalesForecasting.Services;

namespace NopStation.Plugin.Misc.SalesForecasting.Areas.Admin.Controllers
{
    public class SalesForecastingController : NopStationAdminController
    {
        #region Fields

        private readonly ILocalizationService _localizationService;
        private readonly INotificationService _notificationService;
        private readonly IPermissionService _permissionService;
        private readonly ISettingService _settingService;
        private readonly IStoreContext _storeContext;
        private readonly ISalesForecastingService _salesForecastingService;
        private readonly IProductService _productService;
        private readonly ISalesForecastingModelFactory _salesForecastingModelFactory;

        #endregion

        #region Ctor

        public SalesForecastingController(ILocalizationService localizationService,
            INotificationService notificationService,
            IPermissionService permissionService,
            ISettingService settingService,
            IStoreContext storeContext,
            ISalesForecastingService salesForecastingService,
            IProductService productService,
            ISalesForecastingModelFactory salesForecastingModelFactory)
        {
            _localizationService = localizationService;
            _notificationService = notificationService;
            _permissionService = permissionService;
            _settingService = settingService;
            _storeContext = storeContext;
            _salesForecastingService = salesForecastingService;
            _productService = productService;
            _salesForecastingModelFactory = salesForecastingModelFactory;
        }

        #endregion

        #region Methods

        #region Configure
        public async Task<IActionResult> ConfigureAsync()
        {
            if (!await _permissionService.AuthorizeAsync(SalesForecastingProvider.ManageConfiguration))
                return AccessDeniedView();

            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var salesForcastingSettings = await _settingService.LoadSettingAsync<SalesForecastingSettings>(storeScope);

            var model = new ConfigurationModel
            {
                NumberofBestDemandProduct = salesForcastingSettings.NumberofBestDemandProduct,
                FridayIsWeekend = salesForcastingSettings.FridayIsWeekend,
                SaturdayIsWeekend = salesForcastingSettings.SaturdayIsWeekend, 
                SundayIsWeekend = salesForcastingSettings.SundayIsWeekend, 
                MondayIsWeekend = salesForcastingSettings.MondayIsWeekend, 
                TuesdayIsWeekend = salesForcastingSettings.TuesdayIsWeekend, 
                WednesdayIsWeekend = salesForcastingSettings.WednesdayIsWeekend, 
                ThursdayIsWeekend = salesForcastingSettings.ThursdayIsWeekend
            };

            if (storeScope > 0)
            {
                model.NumberofBestDemandProduct_OverrideForStore = await _settingService.SettingExistsAsync(salesForcastingSettings, x => x.NumberofBestDemandProduct, storeScope);
                model.FridayIsWeekend = await _settingService.SettingExistsAsync(salesForcastingSettings, x => x.FridayIsWeekend, storeScope);
                model.SaturdayIsWeekend = await _settingService.SettingExistsAsync(salesForcastingSettings, x => x.SaturdayIsWeekend, storeScope);
                model.SundayIsWeekend = await _settingService.SettingExistsAsync(salesForcastingSettings, x => x.SundayIsWeekend, storeScope);
                model.MondayIsWeekend = await _settingService.SettingExistsAsync(salesForcastingSettings, x => x.MondayIsWeekend, storeScope);
                model.TuesdayIsWeekend = await _settingService.SettingExistsAsync(salesForcastingSettings, x => x.TuesdayIsWeekend, storeScope);
                model.WednesdayIsWeekend = await _settingService.SettingExistsAsync(salesForcastingSettings, x => x.WednesdayIsWeekend, storeScope);
                model.ThursdayIsWeekend = await _settingService.SettingExistsAsync(salesForcastingSettings, x => x.ThursdayIsWeekend, storeScope);
            }

            return View(model);
        }

        [EditAccess, HttpPost]
        public async Task<IActionResult> ConfigureAsync(ConfigurationModel model)
        {
            if (!await _permissionService.AuthorizeAsync(SalesForecastingProvider.ManageConfiguration))
                return AccessDeniedView();

            if (!ModelState.IsValid)
                return await ConfigureAsync();

            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var salesForcastingSettings = await _settingService.LoadSettingAsync<SalesForecastingSettings>(storeScope);

            //save settings
            salesForcastingSettings.NumberofBestDemandProduct = model.NumberofBestDemandProduct;
            salesForcastingSettings.FridayIsWeekend = model.FridayIsWeekend;
            salesForcastingSettings.SaturdayIsWeekend = model.SaturdayIsWeekend;
            salesForcastingSettings.SundayIsWeekend = model.SundayIsWeekend;
            salesForcastingSettings.MondayIsWeekend = model.MondayIsWeekend;
            salesForcastingSettings.TuesdayIsWeekend = model.TuesdayIsWeekend;
            salesForcastingSettings.WednesdayIsWeekend = model.WednesdayIsWeekend;
            salesForcastingSettings.ThursdayIsWeekend = model.ThursdayIsWeekend;
            
            await _settingService.SaveSettingOverridablePerStoreAsync(salesForcastingSettings, x => x.NumberofBestDemandProduct, model.NumberofBestDemandProduct_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(salesForcastingSettings, x => x.FridayIsWeekend, model.FridayIsWeekend_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(salesForcastingSettings, x => x.SaturdayIsWeekend, model.SaturdayIsWeekend_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(salesForcastingSettings, x => x.SundayIsWeekend, model.SundayIsWeekend_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(salesForcastingSettings, x => x.MondayIsWeekend, model.MondayIsWeekend_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(salesForcastingSettings, x => x.TuesdayIsWeekend, model.TuesdayIsWeekend_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(salesForcastingSettings, x => x.WednesdayIsWeekend, model.WednesdayIsWeekend_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(salesForcastingSettings, x => x.ThursdayIsWeekend, model.ThursdayIsWeekend_OverrideForStore, storeScope, false);
            //now clear settings cache
            await _settingService.ClearCacheAsync();

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

            return await ConfigureAsync();
        }
        #endregion

        #region Model
        [EditAccess, HttpPost]
        public async Task<IActionResult> TrainModelAsync()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageProducts))
                return AccessDeniedView();

            bool largeDataSet = false;

            #region Model Directory Preparation
            _salesForecastingService.PathPreparation();
            #endregion

            #region Train Sales Prediction Time series Model
            var trainWeeklyTimeSeriesModelStatus = await _salesForecastingService.TrainWeeklySalesPredictionTimeSeriesModelAsync();

            if (trainWeeklyTimeSeriesModelStatus.Item1)
                _notificationService.SuccessNotification("Training of time series model successfull");
            else
                _notificationService.ErrorNotification("Training of time series model failed");
            #endregion

            #region Train Daily Product Sales Largre Dimension Model
            /// <summary>
            /// Use this model when the dataset is very big otherwise use 
            /// specific and low dimension models
            ///</summary>
            
            if(largeDataSet)
            {
                var trainWeeklyPrductSalesPredictionModelStatus = await _salesForecastingService.TrainLargeFeatureProductSalesPredictionModelAsync();

                if (trainWeeklyPrductSalesPredictionModelStatus.Item1)
                    _notificationService.SuccessNotification("Training of product sales prediction model successfull");
                else
                    _notificationService.ErrorNotification("Training of product sales prediction model failed");
            }

            #endregion

            #region Train Ensemble Learning Model

            if(!largeDataSet)
            {

                #region Train Monthly Product Sales Prediction Base Model (Category specific-Building block of Ensemble learning)

                var categoryWiseBaseModelStatus = await _salesForecastingService.TrainBaseCategoryWiseProductSalesPredictionModelAsync();
                if (categoryWiseBaseModelStatus.Item1)
                    _notificationService.SuccessNotification("Training of Category-Base model successfull");
                else
                    _notificationService.ErrorNotification("Training of Category-Base model failed");

                #endregion

                #region Train Monthly Product Sales Prediction Base Model (Location specific-Building block of Ensemble learning)
                var locationWiseBaseModelStatus = await _salesForecastingService.TrainBaseLocationWiseProductSalesPredictionModelAsync();
                if (locationWiseBaseModelStatus.Item1)
                    _notificationService.SuccessNotification("Training of Location-Base model successfull");
                else
                    _notificationService.ErrorNotification("Training of Location-Base model failed");
                #endregion

                #region Train Monthly Product Sales Prediction Base Model (Category Avg Price specific-Building block of Ensemble learning)
                var categoryAvgPriceWiseBaseModelStatus = await _salesForecastingService.TrainBaseCategoryAvgPriceWiseProductSalesPredictionModelAsync();
                if (categoryAvgPriceWiseBaseModelStatus.Item1)
                    _notificationService.SuccessNotification("Training of Category-Avg Price Base model successfull");
                else
                    _notificationService.ErrorNotification("Training of Category-Avg Price model failed");
                #endregion

                #region Train Monthly Product Sales Prediction Base Model (Month specific-Building block of Ensemble learning)
                var monthWiseBaseModelStatus = await _salesForecastingService.TrainBaseMonthWiseProductSalesPredictionModelAsync();
                if (monthWiseBaseModelStatus.Item1)
                    _notificationService.SuccessNotification("Training of Month wise Base model successfull");
                else
                    _notificationService.ErrorNotification("Training of Month wise Base model failed");
                #endregion
            }

            #endregion

            return RedirectToAction("Configure");
        }

        public async Task<IActionResult> SalesPrediction()
        {
            var searchModel = await _salesForecastingModelFactory.PreparePredictionSearchModel(new PredictionSearchModel());
            return View(searchModel);
        }

        public async Task<IActionResult> GetWeeklySalesByTimeSeriesModelAsync(TestFeatureModel testFeatureModel)
        {
            var predictions = _salesForecastingService.TimeSeriesPredictWeeklySales();
            var prevMonthSalesHistoryPerDay = await _salesForecastingService.DailySalesHistoryQueryLastMonth();
            prevMonthSalesHistoryPerDay.Reverse();
            var salesResponse = new List<JsonResponse>();
            foreach (var eachDay in prevMonthSalesHistoryPerDay)
            {
                salesResponse.Add(new JsonResponse
                {
                    XLabelName = $"{eachDay.Day}.{eachDay.Month}.{eachDay.Year}",
                    YLabelValue = eachDay.NextDayUnits
                });
            }
            salesResponse.Add(new JsonResponse
            {
                XLabelName = "Today",
                YLabelValue = predictions[0]
            });
            var today = DateTime.UtcNow;
            for(int i = 1;i < predictions.Count();i++)
            {
                var currentDay = today.AddDays(i);
                salesResponse.Add(new JsonResponse
                {
                    XLabelName = "",
                    YLabelValue = predictions[i]
                });
                if(currentDay.DayOfWeek == DayOfWeek.Sunday)
                {
                    salesResponse[salesResponse.Count - 1].XLabelName = "Sunday";
                }
                else if (currentDay.DayOfWeek == DayOfWeek.Monday)
                {
                    salesResponse[salesResponse.Count - 1].XLabelName = "Monday";
                }
                else if (currentDay.DayOfWeek == DayOfWeek.Tuesday)
                {
                    salesResponse[salesResponse.Count - 1].XLabelName = "Tuesday";
                }
                else if (currentDay.DayOfWeek == DayOfWeek.Wednesday)
                {
                    salesResponse[salesResponse.Count - 1].XLabelName = "Wednesday";
                }
                else if (currentDay.DayOfWeek == DayOfWeek.Thursday)
                {
                    salesResponse[salesResponse.Count - 1].XLabelName = "Thursday";
                }
                else if (currentDay.DayOfWeek == DayOfWeek.Friday)
                {
                    salesResponse[salesResponse.Count - 1].XLabelName = "Friday";
                }
                else if (currentDay.DayOfWeek == DayOfWeek.Saturday)
                {
                    salesResponse[salesResponse.Count - 1].XLabelName = "Saturday";
                }
            }
            return Json(salesResponse);
        }

        public async Task<IActionResult> GetMonthlyCategoriesCombinedSales(TestFeatureModel testFeatureModel)
        {
            var prevYearSalesHistoryPerMonth = await _salesForecastingService.MonthlySalesHistoryQueryLastYear();
            prevYearSalesHistoryPerMonth.Reverse();
            var prediction = await _salesForecastingService.PredictEnsembleNextMonthSales();
            var salesResponse = new List<JsonResponse>();
            foreach (var eachMonth in prevYearSalesHistoryPerMonth)
            {
                salesResponse.Add(new JsonResponse
                {
                    XLabelName = $"{eachMonth.Month}",
                    YLabelValue = eachMonth.UnitsSoldCurrent
                });
            }
            foreach(var each in salesResponse)
            {
                switch (each.XLabelName)
                {
                    case "1":
                        each.XLabelName = "January";
                        break;
                    case "2":
                        each.XLabelName = "February";
                        break;
                    case "3":
                        each.XLabelName = "March";
                        break;
                    case "4":
                        each.XLabelName = "April";
                        break;
                    case "5":
                        each.XLabelName = "May";
                        break;
                    case "6":
                        each.XLabelName = "June";
                        break;
                    case "7":
                        each.XLabelName = "July";
                        break;
                    case "8":
                        each.XLabelName = "August";
                        break;
                    case "9":
                        each.XLabelName = "September";
                        break;
                    case "10":
                        each.XLabelName = "October";
                        break;
                    case "11":
                        each.XLabelName = "November";
                        break;
                    case "12":
                        each.XLabelName = "December";
                        break;
                    default:
                        break;
                }
            } 
            salesResponse.Add(new JsonResponse
            {
                XLabelName = "Next Month",
                YLabelValue = prediction
            });
            return Json(salesResponse);
        }

        public async Task<IActionResult> GetMonthlyCategoriesCombinedSalesContribution(TestFeatureModel testFeatureModel)
        {
            var prediction = await _salesForecastingService.PredictCategorySalesContribution();
            var salesResponse = new List<JsonResponse>();
            foreach(var eachPrediction in prediction)
            {
                salesResponse.Add(new JsonResponse
                {
                    XLabelName = $"{eachPrediction.CategoryName}",
                    YLabelValue = eachPrediction.contribution
                });
            }
            return Json(salesResponse);
        }
        public async Task<IActionResult> GetMonthlyCategoriesPredictedSalesVsStock(TestFeatureModel testFeatureModel)
        {
            // work here 
            var prediction = await _salesForecastingService.PredictCategorySalesContribution();
            var salesResponse = new List<JsonResponse>();
            foreach(var eachPrediction in prediction)
            {
                salesResponse.Add(new JsonResponse
                {
                    XLabelName = $"{eachPrediction.CategoryName}",
                    YLabelValuePredicted = eachPrediction.quantity, 
                    YLabelValueInStock = (eachPrediction.quantity * 100)%80
                });
            }
            return Json(salesResponse);
        }

        #endregion

        #endregion
    }
}