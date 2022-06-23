using Autodesk.AutoCAD.EditorInput;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using wyDay.TurboActivate;
using AcAp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace EE_Analyzer
{
    public enum UIModes
    {
        MODE_X_DIR_UNDEFINED = -2,
        MODE_Y_DIR_UNDEFINED = -1,
        MODE_X_DIR_SPA      = 0,
        MODE_X_DIR_QTY      = 1,
        MODE_X_DIR_DETAIL   = 2,
        MODE_Y_DIR_SPA      = 3,
        MODE_Y_DIR_QTY      = 4,
        MODE_Y_DIR_DETAIL   = 5
    }


    /// <summary>
    /// Interaction logic for EE_FDNInputDialog.xaml
    /// </summary>
    public partial class EE_FDNInputDialog : Window
    {
        //// licensing
        //readonly TurboActivate ta;
        //bool isGenuine;

        //// Don't use 0 for either of these values.
        //// We recommend 90, 14. But if you want to lower the values we don't recommend going
        //// below 7 days for each value. Anything lower and you're just punishing legit users.
        //const uint DaysBetweenChecks = 90;
        //const uint GracePeriodLength = 14;


        // signals that the mode will use the quantity and spacings as the same for all beams
        public UIModes UI_MODE_X_DIR { get; set; }
        public UIModes UI_MODE_Y_DIR { get; set; }

        private FoundationLayout CurrentFoundationLayout { get; set; }

        public static string VERSION_INFO { get; } = EE_Settings.CURRENT_VERSION_NUM;
        public static string COPYRIGHT_INFO { get; } = EE_Settings.SIGNATURE_LABEL;

        public EE_FDNInputDialog(
            int x_qty = 5, double x_spa=120, double x_width=12, double x_depth=24,
            int y_qty = 7, double y_spa = 120, double y_width = 12, double y_depth = 24,
            int beam_x_strand_qty=2, int slab_x_strand_qty=8, int beam_y_strand_qty=2, int slab_y_strand_qty=8, double neglect_pt_dim=120,
            int x_spa_1_qty =3, int x_spa_2_qty = 0, int x_spa_3_qty = 0, int x_spa_4_qty = 0, int x_spa_5_qty = 0,
            double x_spa_1_spa = 50, double x_spa_2_spa = 0, double x_spa_3_spa = 0, double x_spa_4_spa = 0, double x_spa_5_spa = 0,
            int y_spa_1_qty = 3, int y_spa_2_qty = 0, int y_spa_3_qty = 0, int y_spa_4_qty = 0, int y_spa_5_qty = 0,
            double y_spa_1_spa = 50, double y_spa_2_spa = 0, double y_spa_3_spa = 0, double y_spa_4_spa = 0, double y_spa_5_spa = 0
            )
        {
            InitializeComponent();

            //try
            //{
            //    //TODO: goto the version page at LimeLM and
            //    // paste this GUID here
            //    ta = new TurboActivate("ipeqs63wjubbhgnazqt3527qhcqxk2a");
            //    // Check if we're activated, and every 90 days verify it with the activation servers
            //    // In this example we won't show an error if the activation was done offline
            //    // (see the 3rd parameter of the IsGenuine() function)
            //    // https://wyday.com/limelm/help/offline-activation/
            //    IsGenuineResult gr = ta.IsGenuine(DaysBetweenChecks, GracePeriodLength, true);

            //    isGenuine = gr == IsGenuineResult.Genuine ||
            //                gr == IsGenuineResult.GenuineFeaturesChanged ||

            //                // an internet error means the user is activated but
            //                // TurboActivate failed to contact the LimeLM servers
            //                gr == IsGenuineResult.InternetError;


            //    // If IsGenuineEx() is telling us we're not activated
            //    // but the IsActivated() function is telling us that the activation
            //    // data on the computer is valid (i.e. the crypto-signed-fingerprint matches the computer)
            //    // then that means that the customer has passed the grace period and they must re-verify
            //    // with the servers to continue to use your app.

            //    //Note: DO NOT allow the customer to just continue to use your app indefinitely with absolutely
            //    //      no reverification with the servers. If you want to do that then don't use IsGenuine() or
            //    //      IsGenuineEx() at all -- just use IsActivated().
            //    if (!isGenuine && ta.IsActivated())
            //    {
            //        // We're treating the customer as is if they aren't activated, so they can't use your app.
            //        // However, we show them a dialog where they can reverify with the servers immediately.

            //        ReVerifyNow frmReverify = new ReVerifyNow(ta, DaysBetweenChecks, GracePeriodLength);

            //        if (frmReverify.ShowDialog() == DialogResult.OK)
            //        {
            //            isGenuine = true;
            //        }
            //        else if (!frmReverify.noLongerActivated) // the user clicked cancel and the user is still activated
            //        {
            //            // Just bail out of your app
            //            Environment.Exit(1);
            //            return;
            //        }
            //    }
            //}
            //catch (TurboActivateException ex)
            //{
            //    // failed to check if activated, meaning the customer screwed something up
            //    // so kill the app immediately
            //    MessageBox.Show("Failed to check if activated: " + ex.Message);
            //    Environment.Exit(1);
            //    return;
            //}

            //// Show a trial if we're not genuine
            //// See step 9, below.
            //ShowTrial(!isGenuine);

            DataContext = this;

            BEAM_X_QTY.Text = x_qty.ToString();
            BEAM_X_SPACING.Text = x_spa.ToString();
            BEAM_X_DEPTH.Text = x_depth.ToString();
            BEAM_X_WIDTH.Text = x_width.ToString();
            BEAM_X_STRAND_QTY.Text = beam_x_strand_qty.ToString();
            SLAB_X_STRAND_QTY.Text = slab_x_strand_qty.ToString();
                        
            BEAM_Y_QTY.Text = y_qty.ToString();
            BEAM_Y_SPACING.Text = y_spa.ToString();
            BEAM_Y_DEPTH.Text = y_depth.ToString();
            BEAM_Y_WIDTH.Text = y_width.ToString();
            BEAM_Y_STRAND_QTY.Text = beam_y_strand_qty.ToString();
            SLAB_Y_STRAND_QTY.Text = slab_y_strand_qty.ToString();

            NEGLECT_PT_DIM.Text = neglect_pt_dim.ToString();

            X_SPA_1_QTY.Text = x_spa_1_qty.ToString();
            X_SPA_2_QTY.Text = x_spa_2_qty.ToString();
            X_SPA_3_QTY.Text = x_spa_3_qty.ToString();
            X_SPA_4_QTY.Text = x_spa_4_qty.ToString();
            X_SPA_5_QTY.Text = x_spa_5_qty.ToString();
            X_SPA_1_SPA.Text = x_spa_1_spa.ToString();
            X_SPA_2_SPA.Text = x_spa_2_spa.ToString();
            X_SPA_3_SPA.Text = x_spa_3_spa.ToString();
            X_SPA_4_SPA.Text = x_spa_4_spa.ToString();
            X_SPA_5_SPA.Text = x_spa_5_spa.ToString();

            Y_SPA_1_QTY.Text = y_spa_1_qty.ToString();
            Y_SPA_2_QTY.Text = y_spa_2_qty.ToString();
            Y_SPA_3_QTY.Text = y_spa_3_qty.ToString();
            Y_SPA_4_QTY.Text = y_spa_4_qty.ToString();
            Y_SPA_5_QTY.Text = y_spa_5_qty.ToString();
            Y_SPA_1_SPA.Text = y_spa_1_spa.ToString();
            Y_SPA_2_SPA.Text = y_spa_2_spa.ToString();
            Y_SPA_3_SPA.Text = y_spa_3_spa.ToString();
            Y_SPA_4_SPA.Text = y_spa_4_spa.ToString();
            Y_SPA_5_SPA.Text = y_spa_5_spa.ToString();

            UI_MODE_X_DIR = UIModes.MODE_X_DIR_UNDEFINED;
            UI_MODE_Y_DIR = UIModes.MODE_Y_DIR_UNDEFINED;

        }

        /// <summary>
        /// Handles the 'Click' event of the 'OK' button.
        /// </summary>
        /// <param name="sender">Event source.</param>
        /// <param name="e">Event data.</param>
        private void btnOK_Click(object sender, RoutedEventArgs e)
        {

            DialogResult = true;

            int x_qty = 0;
            double x_spa = 0;
            double x_depth = 0;
            double x_width = 0;
            int beam_x_strand_qty = 0;
            int slab_x_strand_qty = 0;

            int y_qty = 0;
            double y_spa = 0;
            double y_depth = 0;
            double y_width = 0;
            int beam_y_strand_qty = 0;
            int slab_y_strand_qty = 0;

            double neglect_pt_dim = 0;
             
            int x_spa_1_qty = 0;
            int x_spa_2_qty = 0;
            int x_spa_3_qty = 0;
            int x_spa_4_qty = 0;
            int x_spa_5_qty = 0;
            double x_spa_1_spa = 0;
            double x_spa_2_spa = 0;
            double x_spa_3_spa = 0;
            double x_spa_4_spa = 0;
            double x_spa_5_spa = 0;
           
            int y_spa_1_qty = 0;
            int y_spa_2_qty = 0;
            int y_spa_3_qty = 0;
            int y_spa_4_qty = 0;
            int y_spa_5_qty = 0;
            double y_spa_1_spa = 0;
            double y_spa_2_spa = 0;
            double y_spa_3_spa = 0;
            double y_spa_4_spa = 0;
            double y_spa_5_spa = 0;

            bool resultOK = true;
            // Grab the values from the dialog and check for validity
            try
            {
                if (Double.TryParse(BEAM_X_DEPTH.Text, out x_depth) is false)
                {
                    MessageBox.Show("Error reading X-dir beam spacing");
                    resultOK = false;
                }
                if (Double.TryParse(BEAM_X_WIDTH.Text, out x_width) is false)
                {
                    MessageBox.Show("Error reading X-dir beam spacing");
                    resultOK = false;
                }
                if (Double.TryParse(NEGLECT_PT_DIM.Text, out neglect_pt_dim) is false)
                {
                    MessageBox.Show("Error reading minimum length of PT");
                    resultOK = false;
                }

                if (Int32.TryParse(BEAM_X_STRAND_QTY.Text, out beam_x_strand_qty) is false)
                {
                    resultOK = false;
                    MessageBox.Show("Error reading X-dir beam strand qty");
                }
                if (Int32.TryParse(BEAM_Y_STRAND_QTY.Text, out beam_y_strand_qty) is false)
                {
                    resultOK = false;
                    MessageBox.Show("Error reading Y-dir beam strand qty");
                }
                if (Int32.TryParse(SLAB_X_STRAND_QTY.Text, out slab_x_strand_qty) is false)
                {
                    resultOK = false;
                    MessageBox.Show("Error reading X-dir slab strand qty");
                }
                if (Int32.TryParse(SLAB_Y_STRAND_QTY.Text, out slab_y_strand_qty) is false)
                {
                    resultOK = false;
                    MessageBox.Show("Error reading Y-dir slab strand qty");
                }

                
                if (UI_MODE_X_DIR == UIModes.MODE_X_DIR_QTY)
                {
                    if (Int32.TryParse(BEAM_X_QTY.Text, out x_qty) is false)
                    {
                        resultOK = false;
                        MessageBox.Show("Error reading X-dir beam qty");
                    }

                }
                else if (UI_MODE_X_DIR == UIModes.MODE_X_DIR_SPA)
                {
                    if (Double.TryParse(BEAM_X_SPACING.Text, out x_spa) is false)
                    {
                        MessageBox.Show("Error reading X-dir beam spacing");
                        resultOK = false;
                    }
                }
                else if (UI_MODE_X_DIR == UIModes.MODE_X_DIR_DETAIL)
                {
                    if (Int32.TryParse(X_SPA_1_QTY.Text, out x_spa_1_qty) is false)
                    {
                        MessageBox.Show("Error reading X_SPA_1 quantity");
                        resultOK = false;
                    }
                    if (Int32.TryParse(X_SPA_2_QTY.Text, out x_spa_2_qty) is false)
                    {
                        MessageBox.Show("Error reading X_SPA_2 quantity");
                        resultOK = false;
                    }
                    if (Int32.TryParse(X_SPA_3_QTY.Text, out x_spa_3_qty) is false)
                    {
                        MessageBox.Show("Error reading X_SPA_3 quantity");
                        resultOK = false;
                    }
                    if (Int32.TryParse(X_SPA_4_QTY.Text, out x_spa_4_qty) is false)
                    {
                        MessageBox.Show("Error reading X_SPA_4 quantity");
                        resultOK = false;
                    }
                    if (Int32.TryParse(X_SPA_5_QTY.Text, out x_spa_5_qty) is false)
                    {
                        MessageBox.Show("Error reading X_SPA_5 quantity");
                        resultOK = false;
                    }

                    if (Double.TryParse(X_SPA_1_SPA.Text, out x_spa_1_spa) is false)
                    {
                        MessageBox.Show("Error reading X_SPA_1 spacing");
                        resultOK = false;
                    }
                    if (Double.TryParse(X_SPA_2_SPA.Text, out x_spa_2_spa) is false)
                    {
                        MessageBox.Show("Error reading X_SPA_2 spacing");
                        resultOK = false;
                    }
                    if (Double.TryParse(X_SPA_3_SPA.Text, out x_spa_3_spa) is false)
                    {
                        MessageBox.Show("Error reading X_SPA_3 spacing");
                        resultOK = false;
                    }
                    if (Double.TryParse(X_SPA_4_SPA.Text, out x_spa_4_spa) is false)
                    {
                        MessageBox.Show("Error reading X_SPA_4 spacing");
                        resultOK = false;
                    }
                    if (Double.TryParse(X_SPA_5_SPA.Text, out x_spa_5_spa) is false)
                    {
                        MessageBox.Show("Error reading X_SPA_5 spacing");
                        resultOK = false;
                    }
                }
                else
                {
                    throw new NotImplementedException("Invalid Mode in X-Dir");
                }

                // Y-direction modes
                if (Double.TryParse(BEAM_Y_DEPTH.Text, out y_depth) is false)
                {
                    MessageBox.Show("Error reading Y-dir beam spacing");
                    resultOK = false;
                }
                if (Double.TryParse(BEAM_Y_WIDTH.Text, out y_width) is false)
                {
                    MessageBox.Show("Error reading Y-dir beam spacing");
                    resultOK = false;
                }

                if (UI_MODE_Y_DIR == UIModes.MODE_Y_DIR_QTY)
                {
                    if (Int32.TryParse(BEAM_Y_QTY.Text, out y_qty) is false)
                    {
                        MessageBox.Show("Error reading Y-dir beam qty");
                        resultOK = false;
                    }
                }
                else if (UI_MODE_Y_DIR == UIModes.MODE_Y_DIR_SPA)
                {
                    if (Double.TryParse(BEAM_Y_SPACING.Text, out y_spa) is false)
                    {
                        MessageBox.Show("Error reading Y-dir beam spcing");
                        resultOK = false;
                    }
                }
                else if (UI_MODE_Y_DIR == UIModes.MODE_Y_DIR_DETAIL)
                {
                    if (Int32.TryParse(Y_SPA_1_QTY.Text, out y_spa_1_qty) is false)
                    {
                        MessageBox.Show("Error reading Y_SPA_1 quantity");
                        resultOK = false;
                    }
                    if (Int32.TryParse(Y_SPA_2_QTY.Text, out y_spa_2_qty) is false)
                    {
                        MessageBox.Show("Error reading Y_SPA_2 quantity");
                        resultOK = false;
                    }
                    if (Int32.TryParse(Y_SPA_3_QTY.Text, out y_spa_3_qty) is false)
                    {
                        MessageBox.Show("Error reading Y_SPA_3 quantity");
                        resultOK = false;
                    }
                    if (Int32.TryParse(Y_SPA_4_QTY.Text, out y_spa_4_qty) is false)
                    {
                        MessageBox.Show("Error reading Y_SPA_4 quantity");
                        resultOK = false;
                    }
                    if (Int32.TryParse(Y_SPA_5_QTY.Text, out y_spa_5_qty) is false)
                    {
                        MessageBox.Show("Error reading Y_SPA_5 quantity");
                        resultOK = false;
                    }

                    if (Double.TryParse(Y_SPA_1_SPA.Text, out y_spa_1_spa) is false)
                    {
                        MessageBox.Show("Error reading Y_SPA_1 spacing");
                        resultOK = false;
                    }
                    if (Double.TryParse(Y_SPA_2_SPA.Text, out y_spa_2_spa) is false)
                    {
                        MessageBox.Show("Error reading Y_SPA_2 spacing");
                        resultOK = false;
                    }
                    if (Double.TryParse(Y_SPA_3_SPA.Text, out y_spa_3_spa) is false)
                    {
                        MessageBox.Show("Error reading Y_SPA_3 spacing");
                        resultOK = false;
                    }
                    if (Double.TryParse(Y_SPA_4_SPA.Text, out y_spa_4_spa) is false)
                    {
                        MessageBox.Show("Error reading Y_SPA_4 spacing");
                        resultOK = false;
                    }
                    if (Double.TryParse(Y_SPA_5_SPA.Text, out y_spa_5_spa) is false)
                    {
                        MessageBox.Show("Error reading Y_SPA_5 spacing");
                        resultOK = false;
                    }
                }
                else
                {
                    throw new NotImplementedException("Invalid Mode in X-Dir");
                }

                if (resultOK)
                {
                    CurrentFoundationLayout = new FoundationLayout();

                    CurrentFoundationLayout.DrawFoundationDetails(
                        x_qty, x_spa, x_depth, x_width,
                        y_qty, y_spa, y_depth, y_width,
                        beam_x_strand_qty, slab_x_strand_qty, beam_y_strand_qty, slab_y_strand_qty,
                        x_spa_1_qty, x_spa_2_qty, x_spa_3_qty, x_spa_4_qty, x_spa_5_qty,
                        x_spa_1_spa, x_spa_2_spa, x_spa_3_spa, x_spa_4_spa, x_spa_5_spa,
                        y_spa_1_qty, y_spa_2_qty, y_spa_3_qty, y_spa_4_qty, y_spa_5_qty,
                        y_spa_1_spa, y_spa_2_spa, y_spa_3_spa, y_spa_4_spa, y_spa_5_spa, 
                        UI_MODE_X_DIR, UI_MODE_Y_DIR, neglect_pt_dim
                        );
                } else
                {
                    MessageBox.Show("Error parsing dialog window values");
                    return;
                }

            } catch (System.Exception ex)
            {
                MessageBox.Show("Error in reading dialog information:  " + ex.Message);
                return;
            }
        }

        private void UpdateOKButton()
        {
            if(UI_MODE_X_DIR == UIModes.MODE_X_DIR_UNDEFINED || UI_MODE_Y_DIR == UIModes.MODE_Y_DIR_UNDEFINED)
            {
                return;
            } else
            {
                btnOK.IsEnabled = true;
                btnOK.Visibility = Visibility.Visible;
            }
        }

        private void X_Detail_Button_Click(object sender, RoutedEventArgs e)
        {
            UI_MODE_X_DIR = UIModes.MODE_X_DIR_DETAIL;
            btnX_Specify_Detail_Spacings.Background = Brushes.MediumOrchid;
            btnX_Detail_Uniform_Max_Spacing.Background = Brushes.White;
            btnX_Detail_Qty.Background = Brushes.White;

            spX_UNIFORM_MAX_SPA.Visibility = Visibility.Collapsed;
            spX_MAX_QTY.Visibility = Visibility.Collapsed;
            spX_DIR_DETAILS.Visibility = Visibility.Visible;

            UpdateOKButton();
        }

        private void X_Detail_Qty_Button_Click(object sender, RoutedEventArgs e)
        {
            UI_MODE_X_DIR = UIModes.MODE_X_DIR_QTY;
            btnX_Specify_Detail_Spacings.Background = Brushes.White;
            btnX_Detail_Uniform_Max_Spacing.Background = Brushes.White;
            btnX_Detail_Qty.Background = Brushes.MediumOrchid;

            spX_UNIFORM_MAX_SPA.Visibility = Visibility.Collapsed;
            spX_MAX_QTY.Visibility = Visibility.Visible;
            spX_DIR_DETAILS.Visibility = Visibility.Collapsed;

            UpdateOKButton();
        }

        private void X_Detail_Spa_Button_Click(object sender, RoutedEventArgs e)
        {
            UI_MODE_X_DIR = UIModes.MODE_X_DIR_SPA;
            btnX_Specify_Detail_Spacings.Background = Brushes.White;
            btnX_Detail_Uniform_Max_Spacing.Background = Brushes.MediumOrchid;
            btnX_Detail_Qty.Background = Brushes.White;

            spX_UNIFORM_MAX_SPA.Visibility = Visibility.Visible;
            spX_MAX_QTY.Visibility = Visibility.Collapsed;
            spX_DIR_DETAILS.Visibility = Visibility.Collapsed;

            UpdateOKButton();
            UpdateOKButton();

        }

        private void Y_Detail_Button_Click(object sender, RoutedEventArgs e)
        {
            UI_MODE_Y_DIR = UIModes.MODE_Y_DIR_DETAIL;
            btnY_Specify_Detail_Spacings.Background = Brushes.MediumOrchid;
            btnY_Detail_Uniform_Max_Spacing.Background = Brushes.White;
            btnY_Detail_Qty.Background = Brushes.White;

            spY_UNIFORM_MAX_SPA.Visibility = Visibility.Collapsed;
            spY_MAX_QTY.Visibility = Visibility.Collapsed;
            spY_DIR_DETAILS.Visibility = Visibility.Visible;

            UpdateOKButton();
        }

        private void Y_Detail_Qty_Button_Click(object sender, RoutedEventArgs e)
        {
            UI_MODE_Y_DIR = UIModes.MODE_Y_DIR_QTY;
            btnY_Specify_Detail_Spacings.Background = Brushes.White;
            btnY_Detail_Uniform_Max_Spacing.Background = Brushes.White;
            btnY_Detail_Qty.Background = Brushes.MediumOrchid;

            spY_UNIFORM_MAX_SPA.Visibility = Visibility.Collapsed;
            spY_MAX_QTY.Visibility = Visibility.Visible;
            spY_DIR_DETAILS.Visibility = Visibility.Collapsed;

            UpdateOKButton();

        }

        private void Y_Detail_Spa_Button_Click(object sender, RoutedEventArgs e)
        {
            UI_MODE_Y_DIR = UIModes.MODE_Y_DIR_SPA;
            btnY_Specify_Detail_Spacings.Background = Brushes.White;
            btnY_Detail_Uniform_Max_Spacing.Background = Brushes.MediumOrchid;
            btnY_Detail_Qty.Background = Brushes.White;

            spY_UNIFORM_MAX_SPA.Visibility = Visibility.Visible;
            spY_MAX_QTY.Visibility = Visibility.Collapsed;
            spY_DIR_DETAILS.Visibility = Visibility.Collapsed;

            UpdateOKButton();
        }
    }
}
