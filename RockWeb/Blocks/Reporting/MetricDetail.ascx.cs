﻿// <copyright>
// Copyright 2013 by the Spark Development Network
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;
using Rock.Attribute;
using Rock.Web.UI;
using Rock.Security;
using Rock.Constants;
using Rock.Web;


namespace RockWeb.Blocks.Reporting
{
    /// <summary>
    /// 
    /// </summary>
    [DisplayName( "Metric Detail" )]
    [Category( "Reporting" )]
    [Description( "Displays the details of the given metric." )]

    public partial class MetricDetail : RockBlock, IDetailBlock
    {
        #region Base Control Methods

        //  overrides of the base RockBlock methods (i.e. OnInit, OnLoad)

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );

            btnDelete.Attributes["onclick"] = string.Format( "javascript: return Rock.dialogs.confirmDelete(event, '{0}');", Metric.FriendlyTypeName );

            btnSecurity.EntityTypeId = EntityTypeCache.Read( typeof( Rock.Model.Metric ) ).Id; ;

            // Metric supports 0 or more Categories, so the entityType is actually MetricCategory, not Metric
            cpMetricCategories.EntityTypeId = EntityTypeCache.Read( typeof( Rock.Model.MetricCategory ) ).Id; ;
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !Page.IsPostBack )
            {
                // in case called normally
                int? metricId = PageParameter( "MetricId" ).AsInteger( false );

                // in case called from CategoryTreeView
                int? metricCategoryId = PageParameter( "MetricCategoryId" ).AsInteger( false );
                MetricCategory metricCategory = null;
                if ( metricCategoryId.HasValue )
                {
                    if ( metricCategoryId.Value > 0 )
                    {
                        // editing a metric, but get the metricId from the metricCategory
                        metricCategory = new MetricCategoryService( new RockContext() ).Get( metricCategoryId.Value );
                        if ( metricCategory != null )
                        {
                            hfMetricCategoryId.Value = metricCategory.Id.ToString();
                            metricId = metricCategory.MetricId;
                        }
                    }
                    else
                    {
                        if ( !metricId.HasValue )
                        {
                            // adding a new metric
                            metricId = 0;
                        }
                    }
                }

                int? parentCategoryId = PageParameter( "ParentCategoryId" ).AsInteger( false );

                if ( metricId.HasValue )
                {
                    if ( parentCategoryId.HasValue )
                    {
                        ShowDetail( "MetricId", metricId.Value, parentCategoryId );
                    }
                    else
                    {
                        ShowDetail( "MetricId", metricId.Value );
                    }
                }
                else
                {
                    pnlDetails.Visible = false;
                }
            }
        }

        #endregion

        #region Events

        // handlers called by the controls on your block

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            //
        }

        /// <summary>
        /// Handles the Click event of the btnSave control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnSave_Click( object sender, EventArgs e )
        {
            Metric metric;

            var rockContext = new RockContext();
            MetricService metricService = new MetricService( rockContext );
            MetricCategoryService metricCategoryService = new MetricCategoryService( rockContext );

            int metricId = hfMetricId.Value.AsInteger( false ) ?? 0;

            if ( metricId == 0 )
            {
                metric = new Metric();
            }
            else
            {
                metric = metricService.Get( metricId );
            }

            metric.Title = tbTitle.Text;
            metric.Subtitle = tbSubtitle.Text;
            metric.Description = tbDescription.Text;
            metric.IconCssClass = tbIconCssClass.Text;
            metric.SourceValueTypeId = ddlSourceType.SelectedValueAsId();
            metric.XAxisLabel = tbXAxisLabel.Text;
            metric.YAxisLabel = tbYAxisLabel.Text;
            metric.IsCumulative = cbIsCumulative.Checked;

            var personService = new PersonService( rockContext );
            var stewardPerson = personService.Get( ppStewardPerson.SelectedValue ?? 0 );
            metric.StewardPersonAliasId = stewardPerson != null ? stewardPerson.PrimaryAliasId : null;
            var adminPerson = personService.Get( ppAdminPerson.SelectedValue ?? 0 );
            metric.AdminPersonAliasId = adminPerson != null ? adminPerson.PrimaryAliasId : null;
            metric.SourceSql = ceSourceSql.Text;
            metric.DataViewId = ddlDataView.SelectedValueAsId();

            if ( !Page.IsValid )
            {
                return;
            }

            if ( !metric.IsValid )
            {
                // Controls will render the error messages                    
                return;
            }

            if ( !cpMetricCategories.SelectedValuesAsInt().Any() )
            {
                cpMetricCategories.ShowErrorMessage( "Must select at least one category" );
                return;
            }

            // do a WrapTransaction since we are doing multiple SaveChanges()
            RockTransactionScope.WrapTransaction( () =>
            {
                var scheduleService = new ScheduleService( rockContext );
                var schedule = scheduleService.Get( metric.ScheduleId ?? 0 );
                if ( schedule == null )
                {
                    schedule = new Schedule();
                    
                    // make it an "Unnamed" metrics schedule
                    schedule.Name = string.Empty;
                    schedule.CategoryId = new CategoryService( rockContext ).Get( Rock.SystemGuid.Category.SCHEDULE_METRICS.AsGuid() ).Id;
                }

                schedule.iCalendarContent = sbSchedule.iCalendarContent;
                if ( schedule.Id == 0 )
                {
                    scheduleService.Add( schedule );

                    // save to make sure we have a scheduleId
                    rockContext.SaveChanges();
                }

                metric.ScheduleId = schedule.Id;

                if ( metric.Id == 0 )
                {
                    metricService.Add( metric );

                    // save to make sure we have a metricId
                    rockContext.SaveChanges();
                }

                // update MetricCategories for Metric            
                metric.MetricCategories = metric.MetricCategories ?? new List<MetricCategory>();
                var selectedCategoryIds = cpMetricCategories.SelectedValuesAsInt();

                // delete any categories that were removed
                foreach ( var metricCategory in metric.MetricCategories )
                {
                    if ( !selectedCategoryIds.Contains( metricCategory.CategoryId ) )
                    {
                        metricCategoryService.Delete( metricCategory );
                    }
                }

                // add any categories that were added
                foreach ( int categoryId in selectedCategoryIds )
                {
                    if ( !metric.MetricCategories.Any( a => a.CategoryId == categoryId ) )
                    {
                        metricCategoryService.Add( new MetricCategory { CategoryId = categoryId, MetricId = metric.Id } );
                    }
                }

                rockContext.SaveChanges();
            } );

            var qryParams = new Dictionary<string, string>();
            qryParams["MetricId"] = metric.Id.ToString();
            if ( hfMetricCategoryId.ValueAsInt() == 0 )
            {
                int? parentCategoryId = PageParameter( "ParentCategoryId" ).AsInteger();
                int? metricCategoryId = new MetricCategoryService( new RockContext() ).Queryable().Where( a => a.MetricId == metric.Id && a.CategoryId == parentCategoryId ).Select( a => a.Id ).FirstOrDefault();
                hfMetricCategoryId.Value = metricCategoryId.ToString();
            }

            qryParams["MetricCategoryId"] = hfMetricCategoryId.Value;

            NavigateToPage( RockPage.Guid, qryParams );
        }

        /// <summary>
        /// Handles the Click event of the btnCancel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnCancel_Click( object sender, EventArgs e )
        {
            if ( hfMetricId.Value.Equals( "0" ) )
            {
                int? parentCategoryId = PageParameter( "ParentCategoryId" ).AsInteger( false );
                if ( parentCategoryId.HasValue )
                {
                    // Cancelling on Add, and we know the parentCategoryId, so we are probably in treeview mode, so navigate to the current page
                    var qryParams = new Dictionary<string, string>();
                    qryParams["CategoryId"] = parentCategoryId.ToString();
                    NavigateToPage( RockPage.Guid, qryParams );
                }
                else
                {
                    // Cancelling on Add.  Return to Grid
                    NavigateToParentPage();
                }
            }
            else
            {
                // Cancelling on Edit.  Return to Details
                MetricService metricService = new MetricService( new RockContext() );
                Metric metric = metricService.Get( hfMetricId.Value.AsInteger() ?? 0 );
                ShowReadonlyDetails( metric );
            }
        }

        /// <summary>
        /// Handles the Click event of the btnEdit control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnEdit_Click( object sender, EventArgs e )
        {
            MetricService metricService = new MetricService( new RockContext() );
            Metric metric = metricService.Get( hfMetricId.Value.AsInteger() ?? 0 );
            ShowEditDetails( metric );
        }

        /// <summary>
        /// Handles the Click event of the btnDelete control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnDelete_Click( object sender, EventArgs e )
        {
            var rockContext = new RockContext();
            MetricService metricService = new MetricService( rockContext );
            Metric metric = metricService.Get( hfMetricId.Value.AsInteger() ?? 0 );

            // intentionally get metricCategory with new RockContext() so we don't confuse SaveChanges()
            int? parentCategoryId = null;
            var metricCategory = new MetricCategoryService( new RockContext() ).Get( hfMetricCategoryId.ValueAsInt() );
            if (metricCategory != null)
            {
                parentCategoryId = metricCategory.CategoryId;
            }

            if ( metric != null )
            {
                string errorMessage;
                if ( !metricService.CanDelete( metric, out errorMessage ) )
                {
                    mdDeleteWarning.Show( errorMessage, ModalAlertType.Information );
                    return;
                }

                metricService.Delete( metric );
                rockContext.SaveChanges();
            }


            var qryParams = new Dictionary<string, string>();
            if ( parentCategoryId != null )
            {
                qryParams["CategoryId"] = parentCategoryId.ToString();
            }

            NavigateToPage( RockPage.Guid, qryParams );
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the ddlSourceType control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void ddlSourceType_SelectedIndexChanged( object sender, EventArgs e )
        {
            int? sourceValueTypeId = ddlSourceType.SelectedValueAsId();
            var sourceValueType = DefinedValueCache.Read( sourceValueTypeId ?? 0 );
            ceSourceSql.Visible = false;
            ddlDataView.Visible = false;
            if ( sourceValueType != null )
            {
                ceSourceSql.Visible = sourceValueType.Guid == Rock.SystemGuid.DefinedValue.METRIC_SOURCE_VALUE_TYPE_SQL.AsGuid();
                ddlDataView.Visible = sourceValueType.Guid == Rock.SystemGuid.DefinedValue.METRIC_SOURCE_VALUE_TYPE_DATAVIEW.AsGuid();
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Shows the detail.
        /// </summary>
        /// <param name="itemKey">The item key.</param>
        /// <param name="itemKeyValue">The item key value.</param>
        public void ShowDetail( string itemKey, int itemKeyValue )
        {
            ShowDetail( itemKey, itemKeyValue, null );
        }

        /// <summary>
        /// Shows the detail.
        /// </summary>
        /// <param name="itemKey">The item key.</param>
        /// <param name="itemKeyValue">The item key value.</param>
        /// <param name="parentCategoryId">The parent category id.</param>
        public void ShowDetail( string itemKey, int itemKeyValue, int? parentCategoryId )
        {
            pnlDetails.Visible = false;
            if ( !itemKey.Equals( "MetricId" ) )
            {
                return;
            }

            var rockContext = new RockContext();
            var metricService = new MetricService( rockContext );
            Metric metric = null;

            if ( !itemKeyValue.Equals( 0 ) )
            {
                metric = metricService.Get( itemKeyValue );
            }
            else
            {
                metric = new Metric { Id = 0, IsSystem = false };
                metric.MetricCategories = new List<MetricCategory>();
                if ( parentCategoryId.HasValue )
                {
                    var metricCategory = new MetricCategory { CategoryId = parentCategoryId.Value };
                    metricCategory.Category = metricCategory.Category ?? new CategoryService( rockContext ).Get( metricCategory.CategoryId );
                    metric.MetricCategories.Add( metricCategory );
                }
            }

            if ( metric == null || !metric.IsAuthorized( Authorization.VIEW, CurrentPerson ) )
            {
                return;
            }

            pnlDetails.Visible = true;
            hfMetricId.Value = metric.Id.ToString();

            // render UI based on Authorized and IsSystem
            bool readOnly = false;
            nbEditModeMessage.Text = string.Empty;

            if ( metric.IsSystem )
            {
                readOnly = true;
                nbEditModeMessage.Text = EditModeMessage.ReadOnlySystem( Metric.FriendlyTypeName );
            }

            btnSecurity.Visible = metric.IsAuthorized( Authorization.ADMINISTRATE, CurrentPerson );
            btnSecurity.Title = metric.Title;
            btnSecurity.EntityId = metric.Id;

            if ( readOnly )
            {
                btnEdit.Visible = false;
                btnDelete.Visible = false;
                ShowReadonlyDetails( metric );
            }
            else
            {
                btnEdit.Visible = true;
                string errorMessage = string.Empty;
                btnDelete.Visible = metricService.CanDelete( metric, out errorMessage );
                if ( metric.Id > 0 )
                {
                    ShowReadonlyDetails( metric );
                }
                else
                {
                    ShowEditDetails( metric );
                }
            }
        }

        /// <summary>
        /// Shows the edit details.
        /// </summary>
        /// <param name="metric">The metric.</param>
        public void ShowEditDetails( Metric metric )
        {
            if ( metric.Id == 0 )
            {
                lReadOnlyTitle.Text = ActionTitle.Add( Metric.FriendlyTypeName ).FormatAsHtmlTitle();

            }
            else
            {
                lReadOnlyTitle.Text = ActionTitle.Edit( metric.Title ).FormatAsHtmlTitle();
            }

            SetEditMode( true );
            LoadDropDowns();

            tbTitle.Text = metric.Title;
            tbSubtitle.Text = metric.Subtitle;
            tbDescription.Text = metric.Description;
            tbIconCssClass.Text = metric.IconCssClass;
            cpMetricCategories.SetValues( metric.MetricCategories.Select( a => a.Category ) );
            ddlSourceType.SetValue( metric.SourceValueTypeId );
            tbXAxisLabel.Text = metric.XAxisLabel;
            tbYAxisLabel.Text = metric.YAxisLabel;
            cbIsCumulative.Checked = metric.IsCumulative;
            ppStewardPerson.SetValue( metric.StewardPersonAlias != null ? metric.StewardPersonAlias.Person : null );
            ppAdminPerson.SetValue( metric.AdminPersonAlias != null ? metric.AdminPersonAlias.Person : null );
            ceSourceSql.Text = metric.SourceSql;
            sbSchedule.iCalendarContent = metric.Schedule != null ? metric.Schedule.iCalendarContent : null;
            if ( metric.LastRunDateTime != null )
            {
                ltLastRunDateTime.Text = metric.LastRunDateTime.ToRelativeDateString();
            }
            else
            {
                ltLastRunDateTime.Text = "-";
            }

            ddlDataView.SetValue( metric.DataViewId );

            // make sure the control visibility is set based on SourceType
            ddlSourceType_SelectedIndexChanged( null, new EventArgs() );
        }

        /// <summary>
        /// Shows the readonly details.
        /// </summary>
        /// <param name="metric">The metric.</param>
        private void ShowReadonlyDetails( Metric metric )
        {
            SetEditMode( false );
            hfMetricId.SetValue( metric.Id );
            lReadOnlyTitle.Text = metric.Title.FormatAsHtmlTitle();

            DescriptionList descriptionListMain = new DescriptionList();

            descriptionListMain.Add( "Subtitle", metric.Subtitle );
            descriptionListMain.Add( "Description", metric.Description );

            if ( metric.MetricCategories != null && metric.MetricCategories.Any() )
            {
                descriptionListMain.Add( "Categories", metric.MetricCategories.Select( s => s.Category.ToString() ).OrderBy( o => o ).ToList().AsDelimited( "," ) );
            }

            lblMainDetails.Text = descriptionListMain.Html;
        }

        /// <summary>
        /// Sets the edit mode.
        /// </summary>
        /// <param name="editable">if set to <c>true</c> [editable].</param>
        private void SetEditMode( bool editable )
        {
            pnlEditDetails.Visible = editable;
            fieldsetViewDetails.Visible = !editable;

            this.HideSecondaryBlocks( editable );
        }

        /// <summary>
        /// Loads the drop downs.
        /// </summary>
        private void LoadDropDowns()
        {
            RockContext rockContext = new RockContext();
            ddlDataView.Items.Clear();
            var dataviewList = new DataViewService( rockContext ).Queryable().Select(
                s => new
                {
                    s.Id,
                    s.Name
                } ).OrderBy( a => a.Name ).ToList();

            foreach ( var item in dataviewList )
            {
                ddlDataView.Items.Add( new ListItem( item.Name, item.Id.ToString() ) );
            }

            ddlSourceType.Items.Clear();
            foreach ( var item in new DefinedValueService( rockContext ).GetByDefinedTypeGuid( Rock.SystemGuid.DefinedType.METRIC_SOURCE_TYPE.AsGuid() ) )
            {
                ddlSourceType.Items.Add( new ListItem( item.Name, item.Id.ToString() ) );
            }
        }

        #endregion
    }
}