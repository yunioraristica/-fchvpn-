using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Runtime;
using Android.Widget;
using static Android.Manifest;
using System;

namespace fchvpn
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : Activity
    {

        public static MainActivity Instance;


        public ProgressBar progressLoading;
        public TextView loadingInfo;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            Instance = this;

            base.OnCreate(savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            try
            {
                string[] perms = new string[] {
                        Permission.WriteExternalStorage,
                        Permission.ReadExternalStorage,
                        Permission.Internet,
                        Permission.ForegroundService
                };
                RequestPermissions(perms, 200);
            }
            catch { }


            progressLoading = FindViewById<ProgressBar>(Resource.Id.progressInfo);
            loadingInfo = FindViewById<TextView>(Resource.Id.loadingInfo);

            try
            {
                startServiceAndroid service = new startServiceAndroid();
                service.StartForegroundServiceCompat();
            }
            catch(Exception ex)
            {
                Toast.MakeText(this, ex.Message, ToastLength.Long);
            }
        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        public bool CheckPermissionGranted(string perm)
        {
            try
            {
                return CheckSelfPermission(perm) == Android.Content.PM.Permission.Granted;
            }
            catch{}
            return false;
        }

    }
}