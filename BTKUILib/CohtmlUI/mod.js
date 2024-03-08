cvr.menu.prototype.BTKUI = {
    uiRefBTK: {},
    breadcrumbsBTK: [],
    currentPageBTK: "CVRMainQM",
    currentDraggedSliderBTK: {},
    currentSliderBarBTK: {},
    currentSliderKnobBTK: {},
    setSliderFunctionBTK: {},
    pushPageBTK: {},
    changeTabBTK: {},
    updateTitle: {},
    currentMod: "",
    isDraggingBTK: false,
    isDraggingTabRootBTK: false,
    tabRootLastDragNumBTK: {},
    selectedTabRootBTK: {},
    selectedPlayerIDBTK: "",
    selectedPlayerNameBTK: "",
    btkAlertToasts: [],
    btkAlertShown: false,
    btkShowAlertFunc: {},
    btkGetImageBackgroundFunc: {},
    btkKnownEngineFunctions: [],
    btkLastTab: "",
    btkMultiSelectPLMode: false,
    btkCollapseCatFunc: {},
    btkBackFunc: {},
    btkRootPageTarget: "",

    info: function(){
        return {
            name: "BTKUI Library",
            version_major: 0,
            version_minor: 1,
            description: "BTKUI Library",
            author: "BTK Development Team",
            author_id: "",
            external_links: [
            ],
            stylesheets: [
                {filename: "BTKUI.css", modes: ["quickmenu"]},
                {filename: "bootstrap-grid.min.css", modes: ["quickmenu"]}
            ],
            compatibility: [],
            icon: "BTKIcon.png",
            feature_level: 1,
            supported_modes: ["quickmenu"]
        };
    },

    translation: function(menu){
        menu.translations["en"]["btkUI-title"] = "BTKUI";
    },

    register: function(menu){
        uiRefBTK = menu;
        breadcrumbsBTK = [];
        currentPageBTK = "MainPage";
        currentMod = "BTKUI";
        currentDraggedSliderBTK = {};
        currentSliderBarBTK = {};
        currentSliderKnobBTK = {};
        selectedTabRootBTK = {};
        tabRootLastDragNumBTK = {};
        isDraggingBTK = false;
        isDraggingTabRootBTK = false;
        setSliderFunctionBTK = this.btkSliderSetValue;
        pushPageBTK = this.btkPushPage;
        updateTitle = this.btkUpdateTitle;
        selectedPlayerIDBTK = "";
        selectedPlayerNameBTK = "";
        changeTabBTK = this.btkChangeTab;
        btkAlertToasts = [];
        btkAlertShown = false;
        btkShowAlertFunc = this.btkShowAlert;
        btkGetImageBackgroundFunc = this.btkGetImageBackground;
        btkKnownEngineFunctions = [];
        btkLastTab = "";
        btkMultiSelectPLMode = false;
        btkCollapseCatFunc = this.btkCollapseCategory;
        btkBackFunc = this.btkBackFunction;
        btkRootPageTarget = "";

        menu.templates["btkUI-shared"] = {c: "btkUI-shared hide", s:[
                {c: "container btk-popup-container hide", a: {"id": "btkUI-PopupConfirm"}, s:[
                        {c: "row", s: [
                                {c: "col align-self-center", s: [{c: "header", h: "Notice", a: {"id": "btkUI-PopupConfirmHeader"}}]}
                            ]},
                        {c: "content notice-text", h:"This is some text!", a: {"id": "btkUI-PopupConfirmText"}},
                        {c: "control-row", s: [
                                {c: "col", s: [{c: "button", s: [{c: "text", h: "Yes", a: {"id": "btkUI-PopupConfirmOk"}}], x:"btkUI-ConfirmOK"}]},
                                {c: "col", s: [{c: "button", s: [{c: "text", h: "No", a: {"id": "btkUI-PopupConfirmNo"}}], x:"btkUI-ConfirmNO"}]}
                            ]},
                    ]},
                {c: "container btk-popup-container hide", a: {"id": "btkUI-PopupNotice"}, s:[
                        {c:"row", s:[
                                {c:"col align-self-center", s:[{c:"header", h:"Notice", a: {"id": "btkUI-PopupNoticeHeader"}}]}
                            ]},
                        {c: "content notice-text", h:"This is some text!", a: {"id": "btkUI-PopupNoticeText"}},
                        {c:"row justify-content-center", s:[
                                {c:"col offset-4 align-self-center", s:[{c:"button", s:[{c:"text", h:"OK", a: {"id": "btkUI-PopupNoticeOK"}}], x:"btkUI-NoticeClose"}]}
                            ]},
                    ]},
                {c: "container btk-popup-container hide", a: {"id": "btkUI-ContextMenu"}, s:[
                        {c:"row", s:[
                                {c:"col align-self-center", s:[{c:"header", h:"Context Menu", a: {"id": "btkUI-ContextMenuHeader"}}]},
                                {c:"col-1", s:[{c: "icon-close", x: "btkUI-closeContext"}]}
                            ]},
                        {c: "scroll-view", s:[
                                {c: "content scroll-content", s:[], a:{"id": "btkUI-ContextMenu-Content"}},
                                {c: "scroll-marker-v"}
                            ]}
                    ]},
                {c: "container container-tabs container-tabs-left", s:[
                        {c:"row", s:[
                                {c: "col-md-2 justify-content-md-left tab", s:[
                                        {c: "icon"}
                                    ], a:{"id":"btkUI-UserMenu", "data-page": "btkUI-PlayerList"}, x: "btkUI-pushPage"},
                                {c: "col-md-2 justify-content-md-left tab selected", s:[
                                        {c: "tab-content", a:{"id":"btkUI-Tab-CVRQM-Icon"}}
                                    ], a:{"id":"btkUI-Tab-CVRMainQM", "tabTarget": "CVRMainQM"}, x: "btkUI-TabChange"},
                                {c: "col-md justify-content-md-center", s:[
                                        {c:"scroll-bg scroll-bg-left hide", s:[{c:"scroll-overlay", a: {"id": "btkUI-TabScroll-Indicator"}}], a:{"id": "btkUI-TabScroll-Container"}},
                                        {c:"row btkUI-TabScroll", a: {"id": "btkUI-TabRoot"}, s: [
                                        ]},
                                    ]}
                            ]}
                    ], a: {"id": "btkUI-TabContainer"}},
                {c: "container-tooltip hide", s:[{c:"content", h:"tooltip info", a:{"id": "btkUI-Tooltip"}}], a:{"id": "btkUI-TooltipContainer"}},
                {c: "container-alertToast hide", s:[{c:"content", h:"toasty!", a:{"id": "btkUI-AlertToast"}}], a:{"id": "btkUI-AlertToastContainer"}, x:"btkUI-ToastDismiss"}
            ], a: {"id": "btkUI-SharedRoot"}};
        menu.templates["btkUI-menu"] = {c: "btkUI menu-category hide", s: [
                {c: "container container-main", s:[
                        {c:"row", s:[
                                {c:"col", s:[
                                        {c: "header", a:{"id": "btkUI-MenuHeader"}, h: "RootPage"},
                                        {c: "content", s:[
                                                {c: "subtitle", a: {"id": "btkUI-MenuSubtitle"}, h: "This is a page!"},
                                            ]}
                                    ]},
                            ]}
                    ]},
                {c: "container container-controls hide", a:{"id": "btkUI-Settings"}, s:[{c: "scroll-view", s:[{c: "content scroll-content", s:[

                            ]}, {c: "scroll-marker-v"}]}]},
                {c: "container container-controls hide", a:{"id": "btkUI-DropdownPage"}, s:[
                        {c: "row header-section", s:[
                                {c:"col-1", s:[{c: "icon-back", x: "btkUI-Back"}]},
                                {c:"col", s:[{c:"header", h:"Dropdown", a:{"id": "btkUI-DropdownHeader"}}]}
                            ]},
                        {c: "scroll-view", s:[{c: "content-subpage scroll-content", s:[
                                {c:"row", a:{"id": "btkUI-Dropdown-OptionRoot"}},
                            ]}, {c: "scroll-marker-v"}]}]},
                {c: "container container-controls hide", a:{"id": "btkUI-NumberEntry"}, s:[{c: "scroll-view", s:[{c: "content scroll-content", s:[
                                {c: "row", s:[
                                        {c:"col-1", s:[{c: "icon-close", x: "btkUI-Back"}]},
                                        {c:"col", s:[{c:"header", h:"Number Input", a:{"id": "btkUI-NumberInputHeader"}}]}
                                    ]},
                                {c: "row", s:[
                                        {c:"col-2", s:[{c: "int-button", s:[{c:"button-text", h:"1"}], x: "btkUI-NumInput", a:{"str": "1"}}]},
                                        {c:"col-2", s:[{c: "int-button", s:[{c:"button-text", h:"2"}], x: "btkUI-NumInput", a:{"str": "2"}}]},
                                        {c:"col-2", s:[{c: "int-button", s:[{c:"button-text", h:"3"}], x: "btkUI-NumInput", a:{"str": "3"}}]},
                                        {c:"col", s:[{c: "int-box", s:[{a:{"id" : "btkUI-numDisplay", "data-display" : ""}, c:"text", h:""}]}]},
                                        {c:"col-2", s:[{c: "int-button", s:[{c:"icon-checkmark"}], x: "btkUI-NumSubmit"}]},
                                    ]},
                                {c: "row", s:[
                                        {c:"col-2", s:[{c: "int-button", s:[{c:"button-text", h:"4"}], x: "btkUI-NumInput", a:{"str": "4"}}]},
                                        {c:"col-2", s:[{c: "int-button", s:[{c:"button-text", h:"5"}], x: "btkUI-NumInput", a:{"str": "5"}}]},
                                        {c:"col-2", s:[{c: "int-button", s:[{c:"button-text", h:"6"}], x: "btkUI-NumInput", a:{"str": "6"}}]},
                                    ]},
                                {c: "row", s:[
                                        {c:"col-2", s:[{c: "int-button", s:[{c:"button-text", h:"7"}], x: "btkUI-NumInput", a:{"str": "7"}}]},
                                        {c:"col-2", s:[{c: "int-button", s:[{c:"button-text", h:"8"}], x: "btkUI-NumInput", a:{"str": "8"}}]},
                                        {c:"col-2", s:[{c: "int-button", s:[{c:"button-text", h:"9"}], x: "btkUI-NumInput", a:{"str": "9"}}]},
                                    ]},
                                {c: "row", s:[
                                        {c:"col-2", s:[{c: "int-button", s:[{c:"icon-back"}], x: "btkUI-NumBack"}]},
                                        {c:"col-2", s:[{c: "int-button", s:[{c:"button-text", h:"0"}], x: "btkUI-NumInput", a:{"str": "0"}}]},
                                        {c:"col-2", s:[{c: "int-button", s:[{c:"button-text", h:"."}], x: "btkUI-NumInput", a:{"str": "."}}]},
                                    ]},
                            ]}, {c: "scroll-marker-v"}]}]},
                {c: "container container-controls-playerlist hide", a:{"id": "btkUI-PlayerList"}, s:[
                        {c: "row header-section", s:[
                                {c:"col-1", s:[{c: "icon-back", x: "btkUI-Back"}]},
                                {c:"col", s:[{c:"header", h:"Player Selection | 0 Players in World", a: {"id": "btkUI-PlayerListHeaderText"}}]},
                                {c:"col-1", s:[{c: "icon-gear", a:{"data-page": "btkUI-SettingsPage"}, x: "btkUI-pushPage"}]}
                            ]},
                        {c: "scroll-view", s:[{c: "content-subpage scroll-content", s:[
                                    {c: "row", a:{"id": "btkUI-PlayerListContent"}}
                                ]}, {c: "scroll-marker-v"}]}]},
                {c: "container container-controls-playerlist hide", a:{"id": "btkUI-PlayerSelectPage"}, s:[
                        {c: "row header-section", s:[
                                {c:"col-1", s:[{c: "icon-back", x: "btkUI-Back"}]},
                                {c:"col", s:[{c:"header", h:"User", a:{"id": "btkUI-PlayerSelectHeader"}}]}
                            ]},
                        {c: "scroll-view", s:[{c: "content-subpage scroll-content", s: [
                                    {c: "row", a:{"id": "btkUI-PlayerSelectPage-Content"}},
                                ]}, {c: "scroll-marker-v"}]}]},
                {c: "container container-controls-playerlist hide", a:{"id": "btkUI-SettingsPage"}, s:[
                    {c: "row header-section", s:[
                            {c:"col-1", s:[{c: "icon-back", x: "btkUI-Back"}]},
                            {c:"col", s:[{c:"header", h:"BTKUI Settings"}]}
                        ]},
                        {c: "scroll-view", s:[
                                    {c: "content-subpage scroll-content", s:[], a:{"id": "btkUI-SettingsPage-Content"}},
                                {c: "scroll-marker-v"}
                            ]}
                    ]},
                {c: "container container-controls-playerlist hide", a:{"id": "btkUI-DropdownPagePL"}, s:[
                        {c: "row header-section", s:[
                                {c:"col-1", s:[{c: "icon-back", x: "btkUI-Back"}]},
                                {c:"col", s:[{c:"header", h:"Dropdown", a:{"id": "btkUI-DropdownPLHeader"}}]}
                            ]},
                        {c: "scroll-view", s:[{c: "content-subpage scroll-content", s:[
                                    {c:"row", a:{"id": "btkUI-DropdownPL-OptionRoot"}},
                                ]}, {c: "scroll-marker-v"}]}]},
            ], a:{"id":"btkUI-Root"}};

        menu.templates["btkUIRowContent"] = {c:"row justify-content-start", a:{"id": "btkUI-Row-[UUID]", "data-collapsed": "false"}};
        menu.templates["btkSlider"] = {c:"slider-root row", s:[{c:"col-9", s:[{c:"text-title", h:"[slider-name] - [current-value]", a:{"id": "btkUI-SliderTitle-[slider-id]", "data-title": "[slider-name]"}}]}, {c:"col", s:[{c:"resetButton hide", x: "btkUI-SliderReset", s: [{c:"text", h:"Reset"}], a: {"id": "btkUI-SliderReset-[slider-id]", "data-sliderid": "[slider-id]", "data-defaultvalue": "[default-value]"}},]}, {c: "col-12", s:[{c:"slider", s:[{c:"sliderBar", s:[{c:"slider-knob", a:{"id": "btkUI-SliderKnob-[slider-id]"}}], a:{"id": "btkUI-SliderBar-[slider-id]"}}], a:{"id":"btkUI-Slider-[slider-id]", "data-slider-id": "[slider-id]", "data-slider-value": "[current-value]", "data-min": "[min-value]", "data-max": "[max-value]", "data-rounding": "[decimal-point]", "data-default": "[default-value]", "data-allow-reset": "[allow-reset]"}}], a:{"id":"btkUI-Slider-[slider-id]-Tooltip", "data-tooltip": "[tooltip-text]"}}], a: {"id":"btkUI-Slider-[slider-id]-Root"}};
        menu.templates["btkToggle"] = {c:"col-3", a:{"id": "btkUI-Toggle-[toggle-id]-Root"}, s:[{c: "toggle", s:[{c:"row", s:[{c:"col align-content-start", s:[{c:"enable circle", a:{"id": "btkUI-toggle-enable"}}]}, {c:"col align-content-end", s:[{c:"disable circle active", a:{"id": "btkUI-toggle-disable"}}]}]},{c:"text-sm", h:"[toggle-name]", a:{"id": "btkUI-Toggle-[toggle-id]-Text"}}], x: "btkUI-Toggle", a:{"id": "btkUI-Toggle-[toggle-id]", "data-toggle": "[toggle-id]", "data-toggleState": "false", "data-tooltip": "[tooltip-data]"}}]};
        menu.templates["btkButton"] = {c:"col-3", a:{"id": "btkUI-Button-[UUID]"}, s:[{c: "button", s:[{c:"icon", a:{"id": "btkUI-Button-[UUID]-Image"}}, {c:"text", h:"[button-text]", a:{"id": "btkUI-Button-[UUID]-Text"}}], x: "btkUI-ButtonAction", a:{"id": "btkUI-Button-[UUID]-Tooltip","data-tooltip": "[button-tooltip]", "data-action": "[button-action]"}}]};
        menu.templates["btkButtonFullImage"] = {c:"col-3", a:{"id": "btkUI-Button-[UUID]"}, s:[{c: "button-fullImage", s:[{c:"text", h:"[button-text]", a:{"id": "btkUI-Button-[UUID]-Text"}}], x: "btkUI-ButtonAction", a:{"id": "btkUI-Button-[UUID]-Tooltip","data-tooltip": "[button-tooltip]", "data-action": "[button-action]"}}]};
        menu.templates["btkButtonTextOnly"] = {c:"col-3", a:{"id": "btkUI-Button-[UUID]"}, s:[{c: "button-textOnly", s:[{c:"text", h:"[button-text]", a:{"id": "btkUI-Button-[UUID]-Text"}}], x: "btkUI-ButtonAction", a:{"id": "btkUI-Button-[UUID]-Tooltip","data-tooltip": "[button-tooltip]", "data-action": "[button-action]"}}]};
        menu.templates["btkMultiSelectOption"] = {c:"col-12", s: [{c:"dropdown-option", s: [{c:"selection-icon"}, {c:"option-text", h: "[option-text]"}], a: {"id": "btkUI-DropdownOption-[option-index]", "data-index": "[option-index]"}, x: "btkUI-DropdownSelect"}]}
        menu.templates["btkUIRootPage"] = {c: "container container-controls hide", a:{"id": "btkUI-[ModName]-[ModPage]"}, s:[{c: "scroll-view", s:[{c: "content scroll-content", s:[], a:{"id": "btkUI-[ModName]-[ModPage]-Content"}}, {c: "scroll-marker-v"}]}]};
        menu.templates["btkUIPage"] = {c: "container container-controls hide", a:{"id": "btkUI-[ModName]-[ModPage]"}, s:[{c: "row header-section", s:[{c:"col-1", s:[{c: "icon-back", x: "btkUI-Back"}]}, {c:"col", s:[{c:"header", h:"[PageHeader]", a:{"id": "btkUI-[ModName]-[ModPage]-Header"}}]}]}, {c: "scroll-view", s:[{c: "content-subpage scroll-content", s:[], a:{"id": "btkUI-[ModName]-[ModPage]-Content"}}, {c: "scroll-marker-v"}]}]};
        menu.templates["btkUIPagePlayerlist"] = {c: "container container-controls-playerlist hide", a:{"id": "btkUI-[ModName]-[ModPage]"}, s:[{c: "row header-section", s:[{c:"col-1", s:[{c: "icon-back", x: "btkUI-Back"}]}, {c:"col", s:[{c:"header", h:"[PageHeader]", a:{"id": "btkUI-[ModName]-[ModPage]-Header"}}]}]}, {c: "scroll-view", s:[{c: "content-subpage scroll-content", s:[], a:{"id": "btkUI-[ModName]-[ModPage]-Content"}}, {c: "scroll-marker-v"}]}]};
        menu.templates["btkUIRowHeader"] = {c: "row rowBorder", a: {"id": "btkUI-Row-[UUID]-HeaderRoot"}, s:[{c:"col", s:[{c:"header", h:"[Header]", a:{"id": "btkUI-Row-[UUID]-HeaderText"}}]}]};
        menu.templates["btkUIRowHeaderCollapsible"] = {c: "row rowBorder", x: "btkUI-Collapse", a: {"id": "btkUI-Row-[UUID]-HeaderRoot", "data-row": "btkUI-Row-[UUID]"}, s:[{c:"col", s:[{c:"header", h:"[Header]", a:{"id": "btkUI-Row-[UUID]-HeaderText"}}]}, {c: "col-2", s: [{c: "icon-collapse ml-auto", a: {"id": "btkUI-Row-[UUID]-Collapse"}}]}]};
        menu.templates["btkUITab"] = {c: "col-md-2 tab", s:[{c: "tab-content", a:{"id":"btkUI-Tab-[TabName]-Image"}}], a:{"id":"btkUI-Tab-[TabName]", "tabTarget": "btkUI-[TabName]-[PageName]"}, x: "btkUI-TabChange"};
        menu.templates["btkPlayerListEntry"] = {c:"col-3", s:[{c:"button-fullImage", x:"btkUI-SelectPlayer", s:[{c:"text", h:"[player-name]"}], a:{"id": "btkUI-PlayerButton-[player-id]-Icon","data-id": "[player-id]", "data-name": "[player-name]", "data-tooltip": "Open up the player options for [player-name]"}}], a:{"id": "btkUI-PlayerButton-[player-id]"}};

        menu.templates["core-quickmenu"].l.push("btkUI-shared");
        menu.templates["core-quickmenu"].l.push("btkUI-menu");

        uiRefBTK.actions["btkUI-open"] = this.actions.btkOpen;
        uiRefBTK.actions["btkUI-Test"] = this.actions.test;
        uiRefBTK.actions["btkUI-pushPage"] = this.actions.btkPushPage;
        uiRefBTK.actions["btkUI-Back"] = this.actions.btkBack;
        uiRefBTK.actions["btkUI-Home"] = this.actions.btkHome;
        uiRefBTK.actions["btkUI-Toggle"] = this.actions.btkToggle;
        uiRefBTK.actions["btkUI-ButtonAction"] = this.actions.btkButtonAction;
        uiRefBTK.actions["btkUI-ConfirmOK"] = this.actions.btkConfirmOK;
        uiRefBTK.actions["btkUI-ConfirmNO"] = this.actions.btkConfirmNo;
        uiRefBTK.actions["btkUI-NoticeClose"] = this.actions.btkNoticeClose;
        uiRefBTK.actions["btkUI-DropdownSelect"] = this.actions.btkDropdownSelect;
        uiRefBTK.actions["btkUI-NumInput"] = this.actions.btkNumInput;
        uiRefBTK.actions["btkUI-NumBack"] = this.actions.btkNumBack;
        uiRefBTK.actions["btkUI-NumSubmit"] = this.actions.btkNumSubmit;
        uiRefBTK.actions["btkUI-TabChange"] = this.actions.btkTabChange;
        uiRefBTK.actions["btkUI-SelectPlayer"] = this.actions.selectPlayer;
        uiRefBTK.actions["btkUI-ToastDismiss"] = this.actions.btkToastDismiss;
        uiRefBTK.actions["btkUI-SliderReset"] = this.actions.btkSliderReset;
        uiRefBTK.actions["btkUI-Collapse"] = this.actions.btkRowCollapse;

        engine.on("btkModInit", this.btkUILibInit);
        engine.on("btkCreateToggle", this.btkCreateToggle);
        engine.on("btkSetToggleState", this.btkSetToggleState);
        engine.on("btkShowConfirm", this.btkShowConfirmationBox);
        engine.on("btkShowNotice", this.btkShowNoticeBox);
        engine.on("btkCreateSlider", this.btkCreateSlider);
        engine.on("btkSliderSetValue", this.btkSliderSetValue);
        engine.on("btkCreateButton", this.btkCreateButton);
        engine.on("btkOpenMultiSelect", this.btkOpenMultiSelect);
        engine.on("btkOpenNumberInput", this.btkOpenNumberInput);
        engine.on("btkCreatePage", this.btkCreatePage);
        engine.on("btkChangeTab", this.btkChangeTab);
        engine.on("btkUpdateTitle", this.btkUpdateTitle);
        engine.on("btkCreateRow", this.btkCreateRow);
        engine.on("btkUpdateText", this.btkUpdateText);
        engine.on("btkPushPage", this.btkPushPage);
        engine.on("btkAddPlayer", this.btkAddPlayer);
        engine.on("btkRemovePlayer", this.btkRemovePlayer);
        engine.on("btkSliderUpdateSettings", this.btkSliderUpdateSettings);
        engine.on("btkDeleteElement", this.btkDeleteElement);
        engine.on("btkUpdateIcon", this.btkUpdateIcon);
        engine.on("btkUpdateTooltip", this.btkUpdateTooltip);
        engine.on("btkLeaveWorld", this.btkLeaveWorld);
        engine.on("btkAlertToast", this.btkShowAlert);
        engine.on("btkClearChildren", this.btkClearChildren);
        engine.on("btkSetDisabled", this.btkSetDisabled);
        engine.on("btkUpdatePageTitle", this.btkUpdatePageTitle);
        engine.on("btkSetCustomCSS", this.btkSetCustomCSS);
        engine.on("btkCreateCustomGlobal", this.btkCreateCustomGlobal);
        engine.on("btkAddCustomAction", this.btkAddCustomAction);
        engine.on("btkAddCustomEngineFunction", this.btkAddCustomEngineFunction);
        engine.on("btkCreateCustomElementCategory", this.btkCreateCustomElementCategory);
        engine.on("btkCreateTab", this.btkCreateTab);
        engine.on("btkUpdateTab", this.btkUpdateTab);
        engine.on("btkCollapseCategory", this.btkCollapseCategory);
        engine.on("btkBack", this.btkBackFunction);
        engine.on("btkSetHidden", this.btkSetHidden);
    },

    init: function(menu){
        console.log("btkUI Init");

        document.addEventListener('mouseover', this.btkOnHover);
        document.addEventListener('mousemove', this.btkSliderMouseMove);
        document.addEventListener("mouseup", this.btkSliderMouseUp);
        document.addEventListener('mousedown', this.btkSliderMouseDown);

        console.log("btkUI setup scrollable tab bar");

        let tabRoot = document.getElementById("btkUI-TabRoot");
        tabRoot.addEventListener('mousedown', this.btkTabRootMouseDown);
        document.addEventListener("mousemove", this.btkTabRootMouseMove);
        document.addEventListener('mouseup', this.btkTabRootMouseUp);
        
        engine.call("btkUI-UILoaded");
    },

    btkGetImageBackground: function(modName, iconInput) {
        if(iconInput !== null && typeof iconInput === "string" && iconInput.length > 0){
            //Check if it's a URL that we allow
            if(iconInput.startsWith("http")){
                if(iconInput.startsWith("https://files.abidata.io")){
                    return "url('" + iconInput + "')";
                }
            } else {
                return "url('mods/BTKUI/images/" + modName + "/" + iconInput + ".png')";
            }
        }
        return "url('mods/BTKUI/images/Placeholder.png')";
    },

    btkOnHover: function (e){
        targetElement = e.target;
        tooltipInfo = null;

        if(targetElement != null) {

            while (tooltipInfo == null && targetElement != null && targetElement.classList != null && !targetElement.classList.contains("menu-category")) {
                tooltipInfo = targetElement.getAttribute("data-tooltip");
                targetElement = targetElement.parentElement;
            }

            if (tooltipInfo != null) {
                document.getElementById("btkUI-Tooltip").innerHTML = tooltipInfo;

                cvr("#btkUI-TooltipContainer").show();
                return;
            }
        }

        cvr("#btkUI-TooltipContainer").hide();
    },
    btkUILibInit: function (plButtonStyle) {
        cvr("#btkUI-UserMenu").show();
        cvr("#btkUI-SharedRoot").show();

        if(plButtonStyle !== "Tab Bar"){
            let tabCont = document.getElementById("btkUI-TabContainer");
            if(tabCont !== null)
                tabCont.classList.remove("container-tabs-left")
            let scrollCont = document.getElementById("btkUI-TabScroll-Container");
            if(scrollCont !== null)
                scrollCont.classList.remove("scroll-bg-left");
            cvr("#btkUI-UserMenu").hide();
        }

        switch(plButtonStyle){
            case "Replace TTS":
                let ttsElement = document.getElementsByClassName("button-tts");

                if(ttsElement.length === 0){
                    console.error("Couldn't find TTS element! Unable to replace!");
                    return;
                }

                ttsElement = ttsElement[0];

                ttsElement.classList.add("icon-multiuser");
                ttsElement.style.backgroundImage = "url(mods/BTKUI/images/Multiuser.png)";
                ttsElement.style.backgroundSize = "contain";
                ttsElement.style.backgroundPositionY = "0";
                ttsElement.style.backgroundPositionX = "0";

                ttsElement.removeEventListener("click", uiRefBTK.actions["openTTSKeyboard"]);
                ttsElement.addEventListener("click", uiRefBTK.actions["btkUI-pushPage"]);
                ttsElement.setAttribute("data-x", "btkUI-pushPage");
                ttsElement.setAttribute("data-page", "btkUI-PlayerList");
                ttsElement.setAttribute("data-tooltip", "Opens the UILib PlayerList");
                break;
            case "Replace Events":
                let eventButton = document.getElementsByClassName("events");

                if(eventButton.length === 0){
                    console.error("Couldn't find event button! Unable to replace!");
                    return;
                }

                eventButton = eventButton[0];

                eventButton.classList.remove("disabled");

                eventButton.removeEventListener("click", uiRefBTK.actions["notImplementedPage"]);
                eventButton.addEventListener("click", uiRefBTK.actions["btkUI-pushPage"]);
                eventButton.setAttribute("data-x", "btkUI-pushPage");
                eventButton.setAttribute("data-page", "btkUI-PlayerList");
                eventButton.setAttribute("data-tooltip", "Opens the UILib PlayerList");

                let icon = eventButton.querySelector('.icon');
                icon.classList.add("icon-multiuser");
                icon.style.backgroundImage = "url(mods/BTKUI/images/Multiuser.png)";
                icon.style.backgroundSize = "contain";
                icon.style.backgroundPositionY = "0";
                icon.style.backgroundPositionX = "0";

                let label = eventButton.querySelector('.label');
                label.innerHTML = "Playerlist";
                break;
        }
    },

    btkAddPlayer: function(username, userid, userImage, playerCount){
        let playerCheck = document.getElementById("btkUI-PlayerButton-" + userid);

        let plHeader = document.getElementById("btkUI-PlayerListHeaderText");
        plHeader.innerHTML = "Player Selection | " + playerCount + " Players in World";

        if(playerCheck != null) return;

        cvr("#btkUI-PlayerListContent").appendChild(cvr.render(uiRefBTK.templates["btkPlayerListEntry"], {
            "[player-name]": username,
            "[player-id]": userid,
        }, uiRefBTK.templates, uiRefBTK.actions));

        let user = document.querySelector("#btkUI-PlayerButton-" + userid + "-Icon");
        user.style.background = "url('" + userImage + "')";
        user.style.backgroundRepeat = "no-repeat";
        user.style.backgroundSize = "cover";
    },

    btkRemovePlayer: function(userid, playerCount){
        let element = document.querySelector("#btkUI-PlayerButton-" + userid);
        if(element != null){
            element.parentElement.removeChild(element);
        }
        let plHeader = document.getElementById("btkUI-PlayerListHeaderText");
        plHeader.innerHTML = "Player Selection | " + playerCount + " Players in World";
    },

    btkLeaveWorld: function(){
        cvr("#btkUI-PlayerListContent").clear();
        let plHeader = document.getElementById("btkUI-PlayerListHeaderText");
        plHeader.innerHTML = "Player Selection | 0 Players in World";
    },

    btkCreateSlider: function(parent, sliderID, currentValue, categoryMode, settings){
        let parentElement;

        if(!categoryMode)
            parentElement = cvr("#" + parent + "-Content");
        else
            parentElement = cvr("#" + parent);

        if(parentElement === null){
            console.error("parentElement wasn't found! Unable to create slider!")
            return;
        }

        let slider = cvr.render(uiRefBTK.templates["btkSlider"], {
            "[slider-name]": settings.SliderName,
            "[slider-id]": sliderID,
            "[current-value]": currentValue.toFixed(settings.DecimalPlaces),
            "[min-value]": settings.MinValue,
            "[max-value]": settings.MaxValue,
            "[tooltip-text]": settings.SliderTooltip,
            "[decimal-point]": settings.DecimalPlaces,
            "[default-value]": settings.DefaultValue,
            "[allow-reset]": settings.AllowDefaultReset
        }, uiRefBTK.templates, uiRefBTK.actions);

        parentElement.appendChild(slider);

        //Set the slider value using our function
        setSliderFunctionBTK(sliderID, currentValue);
    },

    btkTabRootMouseDown: function(e){
        let targetElement = e.target;

        if(targetElement != null) {
            while (targetElement != null) {
                if(targetElement.id !== "btkUI-TabRoot") {
                    targetElement = targetElement.parentElement;
                    continue;
                }

                break;
            }
        }

        isDraggingTabRootBTK = true;
        selectedTabRootBTK = targetElement;
        tabRootLastDragNumBTK = e.screenX;
    },

    btkTabRootMouseUp: function (e){
        if(selectedTabRootBTK === null && !isDraggingTabRootBTK)
            return;

        isDraggingTabRootBTK = false;
        selectedTabRootBTK = null;
    },

    btkTabRootMouseMove: function(e){
        if(!isDraggingTabRootBTK)
            return;
        if(selectedTabRootBTK === null)
            return;

        selectedTabRootBTK.scrollLeft += tabRootLastDragNumBTK - e.screenX;
        tabRootLastDragNumBTK = e.screenX;

        let scrollInd = document.getElementById("btkUI-TabScroll-Indicator");
        let scrollPercent = (selectedTabRootBTK.scrollLeft / (selectedTabRootBTK.scrollWidth-selectedTabRootBTK.getBoundingClientRect().width))*100;

        if(scrollPercent < 99)
            scrollInd.style.width = scrollPercent + "%";
        else
            scrollInd.style.width = "100%";

        if(scrollPercent < 1){
            scrollInd.style.width = "1%";
        }
    },

    btkSliderMouseDown: function(e){
        let targetElement = e.target;
        let sliderID = null;

        if(targetElement != null) {
            while (sliderID == null && targetElement != null && targetElement.classList != null && !targetElement.classList.contains("menu-category")) {
                sliderID = targetElement.getAttribute("data-slider-id");
                if(sliderID == null)
                    targetElement = targetElement.parentElement;
            }
        }

        if (sliderID == null) return;

        isDraggingBTK = true;
        currentDraggedSliderBTK = targetElement;
        currentSliderKnobBTK = document.getElementById("btkUI-SliderKnob-" + sliderID);
        currentSliderBarBTK = document.getElementById("btkUI-SliderBar-" + sliderID);
    },

    btkSliderMouseUp: function(e){
        if(currentDraggedSliderBTK === null && !isDraggingBTK)
            return;

        currentDraggedSliderBTK = null;
        isDraggingBTK = false;
    },

    btkSliderMouseMove: function(e){
        if(!isDraggingBTK)
            return;
        if(currentDraggedSliderBTK === null)
            return;

        let rect = currentSliderBarBTK.getBoundingClientRect();
        let rectKnob = currentSliderKnobBTK.getBoundingClientRect();
        let start = rect.left;
        let end = rect.right;
        let max = (end - start) - rectKnob.width;
        let current = Math.min(Math.max((e.clientX + 50 - start) - rectKnob.width, 0), max);

        currentSliderKnobBTK.style.left = current + 'px';

        //Update the slider value
        let sliderMin = parseFloat(currentDraggedSliderBTK.getAttribute("data-min"));
        let sliderMax = parseFloat(currentDraggedSliderBTK.getAttribute("data-max"));
        let decimalPoint = parseInt(currentDraggedSliderBTK.getAttribute("data-rounding"));
        let defaultValue = parseFloat(currentDraggedSliderBTK.getAttribute("data-default"));
        let allowReset = (currentDraggedSliderBTK.getAttribute("data-allow-reset") === 'true');

        let newValueNum = (sliderMax - sliderMin) * (current) / (max) + sliderMin;
        let newValue = newValueNum.toFixed(decimalPoint);
        currentDraggedSliderBTK.setAttribute("data-slider-value", newValue);

        let sliderID = currentDraggedSliderBTK.getAttribute("data-slider-id");

        let sliderTitle = document.getElementById("btkUI-SliderTitle-" + sliderID);
        sliderTitle.innerHTML = sliderTitle.getAttribute("data-title") + " - " + newValue;

        let difference = (Math.round((Math.abs(defaultValue) + Number.EPSILON) * 100)/100) - (Math.round((Math.abs(newValueNum) + Number.EPSILON) * 100)/100);

        if(difference > 0.01 || difference < -0.01 && allowReset){
            cvr("#btkUI-SliderReset-" + sliderID).show();
        }
        else{
            cvr("#btkUI-SliderReset-" + sliderID).hide();
        }

        engine.call("btkUI-SliderValueUpdated", sliderID, newValue, false);
    },

    btkSliderSetValue: function (sliderID, value){
        let slider = document.getElementById("btkUI-Slider-" + sliderID);
        let sliderKnob = document.getElementById("btkUI-SliderKnob-" + sliderID);

        value = Number(value);

        if(slider === null || sliderKnob === null){
            console.error("Unable to set slider value for " + sliderID + "!");
            return;
        }

        let sliderMin = parseFloat(slider.getAttribute("data-min"));
        let sliderMax = parseFloat(slider.getAttribute("data-max"));
        let decimalPoint = parseInt(slider.getAttribute("data-rounding"));
        let defaultValue = parseFloat(slider.getAttribute("data-default"));
        let allowReset = (slider.getAttribute("data-allow-reset") === 'true');

        slider.setAttribute("data-slider-value", value);

        let sliderTitle = document.getElementById("btkUI-SliderTitle-" + sliderID);
        if(!Number.isNaN(value))
            sliderTitle.innerHTML = sliderTitle.getAttribute("data-title") + " - " + value.toFixed(decimalPoint);
        else
            sliderTitle.innerHTML = sliderTitle.getAttribute("data-title") + " - " + value;

        let slider0Max = sliderMax - sliderMin;
        let origValue = value;
        value = value - sliderMin;

        let max = 1021-100;
        sliderKnob.style.left = (value / slider0Max)*max + 'px';

        let difference = (Math.round((Math.abs(defaultValue) + Number.EPSILON) * 100)/100) - (Math.round((Math.abs(origValue) + Number.EPSILON) * 100)/100);

        if(difference > 0.01 || difference < -0.01 && allowReset){
            cvr("#btkUI-SliderReset-" + sliderID).show();
        }
        else{
            cvr("#btkUI-SliderReset-" + sliderID).hide();
        }
    },

    btkSliderUpdateSettings: function(sliderID, settings){
          let sliderText = document.getElementById("btkUI-SliderTitle-" + sliderID);
          let sliderData = document.getElementById("btkUI-Slider-" + sliderID);
          let sliderTT = document.getElementById("btkUI-Slider-" + sliderID + "-Tooltip");
          let sliderReset = document.getElementById("btkUI-SliderReset-" + sliderID);

          sliderText.setAttribute("data-title", settings.SliderName);
          sliderData.setAttribute("data-min", settings.MinValue);
          sliderData.setAttribute("data-max", settings.MaxValue);
          sliderTT.setAttribute("data-tooltip", settings.SliderTooltip);
          sliderData.setAttribute("data-rounding", settings.DecimalPlaces);
          sliderData.setAttribute("data-default", settings.DefaultValue);
          sliderReset.setAttribute("data-defaultvalue", settings.DefaultValue);

          let value = Number(sliderData.getAttribute("data-slider-value"));

          if(!Number.isNaN(value))
            sliderText.innerHTML = settings.SliderName + " - " + value.toFixed(settings.DecimalPlaces);
    },

    btkShowConfirmationBox: function(title, content, okText, noText){
        let header = document.getElementById("btkUI-PopupConfirmHeader");
        let text = document.getElementById("btkUI-PopupConfirmText");
        let ok = document.getElementById("btkUI-PopupConfirmOk");
        let no = document.getElementById("btkUI-PopupConfirmNo");

        header.innerHTML = title;
        text.innerHTML = content;

        if(okText === null){
            ok.innerHTML = "Yes";
        }
        else{
            ok.innerHTML = okText;
        }

        if(noText === null){
            no.innerHTML = "No";
        }
        else{
            no.innerHTML = noText;
        }

        cvr("#btkUI-PopupConfirm").show();
    },

    btkShowNoticeBox: function(title, content, okText){
        let header = document.getElementById("btkUI-PopupNoticeHeader");
        let text = document.getElementById("btkUI-PopupNoticeText");
        let ok = document.getElementById("btkUI-PopupNoticeOK");

        header.innerHTML = title;
        text.innerHTML = content;

        if(okText === null){
            ok.innerHTML = "Yes";
        }
        else{
            ok.innerHTML = okText;
        }

        cvr("#btkUI-PopupNotice").show();
    },

    btkCreateToggle: function(parentID, toggleName, toggleID, tooltip, state)
    {
        let target = cvr("#" + parentID);

        if(target == null) {
            console.error("Attempted to create a toggle in a parent that doesn't exist! - " + parentID);
            return;
        }

        target.appendChild(cvr.render(uiRefBTK.templates["btkToggle"], {
            "[toggle-name]": toggleName,
            "[toggle-id]": toggleID,
            "[tooltip-data]": tooltip,
        }, uiRefBTK.templates, uiRefBTK.actions));

        newToggle = document.getElementById("btkUI-Toggle-" + toggleID);

        let enabled = newToggle.querySelector("#btkUI-toggle-enable");
        let disabled = newToggle.querySelector("#btkUI-toggle-disable");

        if(state){
            enabled.classList.add("active");
            disabled.classList.remove("active");
        }
        else{
            enabled.classList.remove("active");
            disabled.classList.add("active");
        }

        newToggle.setAttribute("data-toggleState", state.toString());
    },

    btkSetToggleState: function(toggleID, state){
        let element = document.getElementById(toggleID);

        let enabled = element.querySelector("#btkUI-toggle-enable");
        let disabled = element.querySelector("#btkUI-toggle-disable");

        if(state){
            enabled.classList.add("active");
            disabled.classList.remove("active");
        }
        else{
            enabled.classList.remove("active");
            disabled.classList.add("active");
        }

        element.setAttribute("data-toggleState", state.toString());
    },

    btkCreateButton: function(parent, buttonName, buttonIcon, tooltip, buttonUUID, modName, buttonStyle = 0){
        let style = "btkButton";

        switch(buttonStyle){
            case 0:
                style = "btkButton";
                break;
            case 1:
                style = "btkButtonTextOnly";
                break;
            case 2:
                style = "btkButtonFullImage";
                break;
        }

        cvr("#" + parent).appendChild(cvr.render(uiRefBTK.templates[style], {
            "[button-text]": buttonName,
            "[button-tooltip]": tooltip,
            "[button-action]": buttonUUID,
            "[UUID]": buttonUUID,
        }, uiRefBTK.templates, uiRefBTK.actions));

        const buttonBgImage = btkGetImageBackgroundFunc(modName, buttonIcon);

        if(buttonStyle === 0) {
            let button = document.getElementById("btkUI-Button-" + buttonUUID + "-Image");
            button.style.backgroundImage = buttonBgImage;
            button.style.backgroundRepeat = "no-repeat";
            button.style.backgroundSize = "contain";
        }

        if(buttonStyle === 2){
            let button = document.getElementById("btkUI-Button-" + buttonUUID + "-Tooltip");
            button.style.backgroundImage = buttonBgImage;
            button.style.backgroundRepeat = "no-repeat";
            button.style.backgroundSize = "cover";
        }
    },
    btkOpenMultiSelect: function(name, options, selectedIndex, playerListMode = false){
        btkMultiSelectPLMode = playerListMode;

        let element = cvr("#btkUI-Dropdown-OptionRoot");
        if(playerListMode)
            element = cvr("#btkUI-DropdownPL-OptionRoot");
        element.clear();

        for(let i=0; i<options.length; i++){
            let option = element.appendChild(cvr.render(uiRefBTK.templates["btkMultiSelectOption"], {
                "[option-text]": options[i],
                "[option-index]": i,
            }, uiRefBTK.templates, uiRefBTK.actions));

            let optionIcon = option.querySelector(".selection-icon");
            if(i!==selectedIndex)
                optionIcon.classList.remove("selected");
            if(i===selectedIndex)
                optionIcon.classList.add("selected");
        }

        if(!playerListMode)
            cvr("#btkUI-DropdownHeader").innerHTML(name);
        else
            cvr("#btkUI-DropdownPLHeader").innerHTML(name);

        if(!playerListMode)
            cvr("#btkUI-DropdownPage").show();
        else
            cvr("#btkUI-DropdownPagePL").show();
        cvr("#" + currentPageBTK).hide();

        breadcrumbsBTK.push(currentPageBTK);

        engine.call("btkUI-OpenedPage", "DropdownPage", currentPageBTK);

        currentPageBTK = "btkUI-DropdownPage";

        if(playerListMode)
            currentPageBTK = "btkUI-DropdownPagePL";
    },
    btkOpenNumberInput: function(name, number) {
        let display = document.getElementById("btkUI-numDisplay");
        let data = parseFloat(number);
        display.setAttribute("data-display", data);
        display.innerHTML = data.toFixed(3);

        cvr("#btkUI-NumberInputHeader").innerHTML("Editing: " + name);

        uiRefBTK.core.playSoundCore("Click");

        cvr("#btkUI-NumberEntry").show();
        cvr("#" + currentPageBTK).hide();

        breadcrumbsBTK.push(currentPageBTK);

        engine.call("btkUI-OpenedPage", "NumberEntry", currentPageBTK);

        currentPageBTK = "btkUI-NumberEntry";
    },

    btkPushPage: function (targetPage, resetBreadcrumbs = false, modPage = currentMod){
        if(currentPageBTK === targetPage)
            return;

        uiRefBTK.core.switchCategorySelected("btkUI");

        cvr("#" + targetPage).show();
        if(currentPageBTK.length > 0)
            cvr("#" + currentPageBTK).hide();

        breadcrumbsBTK.push(currentPageBTK);

        engine.call("btkUI-OpenedPage", targetPage, currentPageBTK);

        currentPageBTK = targetPage;

        if(modPage !== currentMod || resetBreadcrumbs){
            //We're going to a new root page, clear the breadcrumbs
            currentMod = modPage;

            breadcrumbsBTK.length = 0;

            if(resetBreadcrumbs) {
                breadcrumbsBTK.push(btkRootPageTarget);
                return;
            }

            breadcrumbsBTK.push(targetPage);
        }
    },

    btkUpdateText: function (elementID, text){
        let element = cvr("#" + elementID);

        if(element === null){
            console.error("Unable to update text of element " + elementID);
            return;
        }

        element.innerHTML(text);
    },

    btkCreateRow: function (parentID, rowUUID, collapsible, collapsed, rowHeader = null){
        let headerGenerate = rowHeader !== null && rowHeader.match(/^ *$/) === null;

        if(headerGenerate){
            if(!collapsible) {
                cvr("#" + parentID + "-Content").appendChild(cvr.render(uiRefBTK.templates["btkUIRowHeader"], {
                    "[UUID]": rowUUID,
                    "[Header]": rowHeader
                }, uiRefBTK.templates, uiRefBTK.actions));
            }
            else {
                cvr("#" + parentID + "-Content").appendChild(cvr.render(uiRefBTK.templates["btkUIRowHeaderCollapsible"], {
                    "[UUID]": rowUUID,
                    "[Header]": rowHeader
                }, uiRefBTK.templates, uiRefBTK.actions));
            }
        }

        cvr("#" + parentID + "-Content").appendChild(cvr.render(uiRefBTK.templates["btkUIRowContent"], {
            "[UUID]": rowUUID
        }, uiRefBTK.templates, uiRefBTK.actions));

        if(headerGenerate && collapsed)
            btkCollapseCatFunc("btkUI-Row-" + rowUUID, true);
    },

    btkCreateTab: function (pageName, modName, tabIcon, cleanedPageName){
        //Check if tab exists to avoid generating duplicates
        let element = document.getElementById("btkUI-Tab-" + modName);

        if(element !== null){
            console.error("Tab for " + modName + " already exists! Tell this mods dev to switch to Page.GetOrCreatePage to avoid creating duplicate root pages!");
            return;
        }

        cvr("#btkUI-TabRoot").appendChild(cvr.render(uiRefBTK.templates["btkUITab"], {
            "[TabName]": modName,
            "[PageName]": cleanedPageName
        }, uiRefBTK.templates, uiRefBTK.actions));

        if (tabIcon !== null && tabIcon.length > 0) {
            let tab = document.getElementById("btkUI-Tab-" + modName + "-Image");
            tab.style.backgroundImage = "url('mods/BTKUI/images/" + modName + "/" + tabIcon + ".png')";
            tab.style.backgroundRepeat = "no-repeat";
            tab.style.backgroundSize = "contain";
        } else {
            let tab = document.getElementById("btkUI-Tab-" + modName + "-Image");
            tab.style.backgroundImage = "url('mods/BTKUI/images/Placeholder.png')";
            tab.style.backgroundRepeat = "no-repeat";
            tab.style.backgroundSize = "contain";
        }

        let tabRoot = document.getElementById("btkUI-TabRoot");
        tabRoot.scrollLeft = 0;

        let scrollInd = document.getElementById("btkUI-TabScroll-Indicator");
        scrollInd.style.width = "1%";

        if(document.getElementById("btkUI-TabRoot").childElementCount >= 8)
            cvr("#btkUI-TabScroll-Container").show();
        else
            cvr("#btkUI-TabScroll-Container").hide();
    },

    btkUpdateTab: function (modName, state){
        let target = document.getElementById("btkUI-Tab-" + modName);

        if(target === null) return;

        if(state){
            if(target.classList.contains("hide")) return;
            target.classList.add("hide");
        } else {
            target.classList.remove("hide");
        }
    },

    btkCreatePage: function (pageName, modName, tabIcon, elementID, rootPage, cleanedPageName, inPlayerlist){
        let elementCheck = null;

        elementCheck = document.getElementById(elementID);

        if(elementCheck !== null) return;

        if(!rootPage) {
            let style = "btkUIPage";

            if(inPlayerlist)
                style = "btkUIPagePlayerlist";

            cvr("#btkUI-Root").appendChild(cvr.render(uiRefBTK.templates[style], {
                "[ModName]": modName,
                "[ModPage]": cleanedPageName,
                "[PageHeader]": pageName,
            }, uiRefBTK.templates, uiRefBTK.actions));

            return;
        }

        cvr("#btkUI-Root").appendChild(cvr.render(uiRefBTK.templates["btkUIRootPage"], {
            "[ModName]": modName,
            "[ModPage]": cleanedPageName,
        }, uiRefBTK.templates, uiRefBTK.actions));
    },

    btkUpdatePageTitle: function(elementID, title){
        headerCheck = cvr("#" + elementID + "-Header");

        if(headerCheck === null) return;

        headerCheck.innerHTML(title);
    },

    btkChangeTab: function (rootTarget, rootMod, menuTitle, menuSubtitle){
        console.log("Setting to rootTarget " + rootTarget + " | currentMod = " + currentMod + " | rootMod = " + rootMod);

        btkLastTab = rootTarget;

        //Clean things up before changing roots
        if(currentPageBTK.length > 0){
            cvr("#" + currentPageBTK).hide();
        }

        if(rootTarget === "CVRMainQM"){
            uiRefBTK.core.switchCategorySelected("quickmenu-home");
            currentMod = "CVR";
            currentPageBTK = "CVRMainQM";
            return;
        }

        uiRefBTK.core.switchCategorySelected("btkUI");

        currentPageBTK = "";
        btkRootPageTarget = rootTarget;

        let targetTab = document.getElementById("btkUI-Tab-" + rootMod);

        if(targetTab !== null) {
            var tabs = document.querySelectorAll(".container-tabs .tab");
            for (let i = 0; i < tabs.length; i++) {
                let tab = tabs[i];
                tab.classList.remove("selected");
            }

            targetTab.classList.add("selected");
        }

        updateTitle(menuTitle, menuSubtitle);

        pushPageBTK(rootTarget, false, rootMod);
    },

    btkUpdateTitle: function (menuTitle, menuSubtitle){
        cvr("#btkUI-MenuHeader").innerHTML(menuTitle);
        cvr("#btkUI-MenuSubtitle").innerHTML(menuSubtitle);
    },

    btkDeleteElement: function (elementID){
        let element = document.getElementById(elementID);

        if(element === null) return;

        element.parentElement.removeChild(element);

        if(document.getElementById("btkUI-TabRoot").childElementCount >= 8)
            cvr("#btkUI-TabScroll-Container").show();
        else
            cvr("#btkUI-TabScroll-Container").hide();
    },

    btkUpdateIcon: function (elementID, modName, icon, suffix = "Image") {
        let element = document.getElementById(elementID + "-" + suffix);

        if(element === null){
            console.log("Unable to find element with ID " + elementID + "-" + suffix + " unable to update icon!");
            return;
        }

        element.style.backgroundImage = btkGetImageBackgroundFunc(modName, icon);
        element.style.backgroundRepeat = "no-repeat";
        element.style.backgroundSize = "contain";
    },

    btkUpdateTooltip: function (elementID, tooltipText){
        let element = document.getElementById(elementID);

        if(element === null){
            console.log("Unable to find element with ID " + elementID + " unable to update tooltip!");
            return;
        }

        element.setAttribute("data-tooltip", tooltipText);
    },

    btkShowAlert: function (message, delay = 5){
        if(btkAlertShown){
            btkAlertToasts.push({"Message": message, "Delay": delay});
            return;
        }

        cvr("#btkUI-AlertToast").innerHTML(message);
        cvr("#btkUI-AlertToastContainer").show();
        btkAlertShown = true;

        setTimeout(() => {
            cvr("#btkUI-AlertToastContainer").hide();
            btkAlertShown = false;
            if(btkAlertToasts.length > 0){
                let alert = btkAlertToasts.shift();
                btkShowAlertFunc(alert.Message, alert.Delay);
            }
        }, delay*1000);
    },

    btkClearChildren: function(targetID){
        let target = cvr("#" + targetID);
        if(target === null) return;

        target.clear();
    },

    btkSetDisabled: function(targetID, state){
        let target = document.getElementById(targetID);

        if(target === null) return;

        if(state){
            if(target.classList.contains("disabled")) return;
            target.classList.add("disabled");
        } else {
            target.classList.remove("disabled");
        }
    },

    btkSetHidden: function(targetID, state){
        let target = document.getElementById(targetID);

        if(target === null) return;

        if(state){
            if(target.classList.contains("hide")) return;
            target.classList.add("hide");
        } else {
            target.classList.remove("hide");
        }
    },

    btkSetCustomCSS: function(cssData) {
        let style = document.createElement('style');
        style.appendChild(document.createTextNode(cssData));
        document.getElementsByTagName('head')[0].appendChild(style);
    },

    //BTKUILib custom element functions

    btkAddCustomEngineFunction: function(functionName, functionCode, functionParams){
        if(btkKnownEngineFunctions.includes(functionName) || functionName.startsWith("btk")) return;

        console.log("Creating custom function " + functionName + " with code " + functionCode + " that has " + functionParams.length + " parameters");

        let createdFunction = null;

        switch(functionParams.length){
            case 0:
                createdFunction = new Function(functionCode);
                break;
            case 1:
                createdFunction = new Function(functionParams[0], functionCode);
                break;
            case 2:
                createdFunction = new Function(functionParams[0], functionParams[1], functionCode);
                break;
            case 3:
                createdFunction = new Function(functionParams[0], functionParams[1], functionParams[2], functionCode);
                break;
            case 4:
                createdFunction = new Function(functionParams[0], functionParams[1], functionParams[2], functionParams[3], functionCode);
                break;
            case 5:
                createdFunction = new Function(functionParams[0], functionParams[1], functionParams[2], functionParams[3], functionParams[4], functionCode);
                break;
            case 6:
                createdFunction = new Function(functionParams[0], functionParams[1], functionParams[2], functionParams[3], functionParams[4], functionParams[5], functionCode);
                break;
            case 7:
                createdFunction = new Function(functionParams[0], functionParams[1], functionParams[2], functionParams[3], functionParams[4], functionParams[5], functionParams[6], functionCode);
                break;
            case 8:
                createdFunction = new Function(functionParams[0], functionParams[1], functionParams[2], functionParams[3], functionParams[4], functionParams[5], functionParams[6], functionParams[7], functionCode);
                break;
        }

        engine.on(functionName, createdFunction);
    },

    btkAddCustomAction: function(actionName, actionCode){
        if(uiRefBTK.actions.includes(actionName)) return;

        console.log("Creating custom action " + actionName + " with code " + actionCode);

        uiRefBTK.actions[actionName] = new Function("e", actionCode);
    },

    btkCreateCustomGlobal: function(uuid, template){
          cvr("#btkUI-SharedRoot").appendChild(cvr.render(JSON.parse(template), {
              "[UUID]": uuid
          }, uiRefBTK.templates, uiRefBTK.actions));
    },

    btkCreateCustomElementCategory: function(parent, uuid, template){
        if(parent == null) {
            console.error("Attempted to create a custom element in a parent that doesn't exist! - " + parent);
            return;
        }

        cvr("#" + parent).appendChild(cvr.render(JSON.parse(template), {
            "[UUID]": uuid
        }, uiRefBTK.templates, uiRefBTK.actions));
    },

    btkCollapseCategory: function(rowTarget, state = null){
        let rowElement = document.getElementById(rowTarget);
        let collapseElement = document.getElementById(rowTarget + "-Collapse");

        if(rowTarget === null)
            return;

        if(state === null) {
            state = (rowElement.getAttribute("data-collapsed") === 'true');
            state = !state;
        }

        if (state) {
            rowElement.classList.add("hide");
            collapseElement.classList.add("icon-expand");
            collapseElement.classList.remove("icon-collapse");
        } else {
            rowElement.classList.remove("hide");
            collapseElement.classList.remove("icon-expand");
            collapseElement.classList.add("icon-collapse");
        }

        rowElement.setAttribute("data-collapsed", state.toString());

        engine.call("btkUI-CollapseCategory", rowTarget, state);
    },

    btkBackFunction: function(){
        let target = breadcrumbsBTK.pop();

        if(target === "")
            target = "CVRMainQM";

        cvr("#" + target).show();
        cvr("#" + currentPageBTK).hide();

        if(target === "CVRMainQM")
            uiRefBTK.core.switchCategorySelected("quickmenu-home");

        engine.call("btkUI-BackAction", target, currentPageBTK);

        currentPageBTK = target;
    },

    actions: {
        btkOpen: function(){
            uiRefBTK.core.playSoundCore("Click");
            uiRefBTK.core.switchCategorySelected("btkUI");
            engine.call("btkUI-OpenMainMenu");
        },
        btkTabChange: function(e){
            uiRefBTK.core.playSoundCore("Click");

            let target = e.currentTarget.getAttribute("tabTarget");

            if(target === null) {
                console.error("Tab did not have a tabTarget!" + e);
                return;
            }

            var tabs = document.querySelectorAll(".container-tabs .tab");
            for(let i=0; i < tabs.length; i++){
                let tab = tabs[i];
                tab.classList.remove("selected");
            }

            e.currentTarget.classList.add("selected");

            engine.call("btkUI-TabChange", target);
        },
        btkPushPage: function (e){
            uiRefBTK.core.playSoundCore("Click");

            let target = e.currentTarget.getAttribute("data-page");

            pushPageBTK(target);
        },
        btkBack: function (){
            uiRefBTK.core.playSoundCore("Click");

            btkBackFunc();
        },
        btkHome: function (){
            uiRefBTK.core.playSoundCore("Click");

            cvr("#" + currentPageBTK).hide();
            currentPageBTK = "";

            var tabs = document.querySelectorAll(".container-tabs .tab");
            for(let i=0; i < tabs.length; i++){
                let tab = tabs[i];
                tab.classList.remove("selected");

                if(tab.id === "btkUI-Tab-CVRMainQM")
                    tab.classList.add("selected");
            }

            changeTabBTK("CVRMainQM", "", "", "")
        },
        btkButtonAction: function (e){
            uiRefBTK.core.playSoundCore("Click");
            let action = e.currentTarget.getAttribute("data-action");

            if(action != null){
                engine.call("btkUI-ButtonAction", action);
            }
        },
        btkNumInput: function (e){
            let str = e.currentTarget.getAttribute("str");
            let display = document.getElementById("btkUI-numDisplay");
            let data = display.getAttribute("data-display");
            if (str === "." && data.includes(".")){
                return;
            }
            if (data.length >= 5){
                return;
            }
            data = data + str;
            display.setAttribute("data-display", data);
            display.innerHTML = data;
        },
        btkNumSubmit: function (){
            let display = document.getElementById("btkUI-numDisplay");
            let data = display.getAttribute("data-display");

            if(data != null){
                engine.call("btkUI-NumSubmit", data);
            }
            uiRefBTK.core.playSoundCore("Click");

            let target = breadcrumbsBTK.pop();

            cvr("#" + target).show();
            cvr("#" + currentPageBTK).hide();

            engine.call("btkUI-BackAction", target, currentPageBTK);

            currentPageBTK = target;
        },
        btkNumBack: function (){
            let display = document.getElementById("btkUI-numDisplay");
            let data = display.getAttribute("data-display");
            data = data.slice(0, -1);
            display.setAttribute("data-display", data);
            display.innerHTML = data;
        },
        btkToggle: function(e){
            uiRefBTK.core.playSoundCore("Click");

            let enabled = e.currentTarget.querySelector("#btkUI-toggle-enable");
            let disabled = e.currentTarget.querySelector("#btkUI-toggle-disable");

            let toggleID = e.currentTarget.getAttribute("data-toggle");
            let state = (e.currentTarget.getAttribute("data-toggleState") === 'true');

            state = !state;

            if(state){
                enabled.classList.add("active");
                disabled.classList.remove("active");
            }
            else{
                enabled.classList.remove("active");
                disabled.classList.add("active");
            }

            e.currentTarget.setAttribute("data-toggleState", state.toString());

            engine.call("btkUI-Toggle", toggleID, state);
        },
        btkSliderReset: function(e){
            let sliderID = e.currentTarget.getAttribute("data-sliderid");
            let defaultValue = e.currentTarget.getAttribute("data-defaultvalue");

            setSliderFunctionBTK(sliderID, defaultValue);
            engine.call("btkUI-SliderValueUpdated", sliderID, defaultValue, true);
        },
        btkDropdownSelect: function(e){
            uiRefBTK.core.playSoundCore("Click");
            let dropdown = document.getElementById("btkUI-Dropdown-OptionRoot");
            if(btkMultiSelectPLMode)
                dropdown = document.getElementById("btkUI-DropdownPL-OptionRoot");
            let options = dropdown.getElementsByClassName("dropdown-option");
            let index = parseInt(e.currentTarget.getAttribute("data-index"));

            for(let i=0; i<options.length; i++){
                let option = options[i];
                let optionIcon = option.querySelector(".selection-icon");
                if(i!==index)
                    optionIcon.classList.remove("selected");
                if(i===index)
                    optionIcon.classList.add("selected");
            }

            engine.call("btkUI-DropdownSelected", index);
        },
        selectPlayer: function(e){
            uiRefBTK.core.playSoundCore("Click");

            if(currentPageBTK === "btkUI-PlayerSelectPage")
                return;

            let playerID = e.currentTarget.getAttribute("data-id");
            let playerName = e.currentTarget.getAttribute("data-name");

            selectedPlayerNameBTK = playerName;
            selectedPlayerIDBTK = playerID;

            cvr("#btkUI-PlayerSelectPage").show();
            cvr("#" + currentPageBTK).hide();

            breadcrumbsBTK.push(currentPageBTK);
            currentPageBTK = "btkUI-PlayerSelectPage";

            engine.call("btkUI-SelectedPlayer", selectedPlayerNameBTK, selectedPlayerIDBTK);

            cvr("#btkUI-PlayerSelectHeader").innerHTML(playerName);
        },
        btkConfirmOK: function (){
            uiRefBTK.core.playSoundCore("Click");
            engine.call("btkUI-PopupConfirmOK");
            cvr("#btkUI-PopupConfirm").hide();
        },
        btkConfirmNo: function (){
            uiRefBTK.core.playSoundCore("Click");
            engine.call("btkUI-PopupConfirmNo");
            cvr("#btkUI-PopupConfirm").hide();
        },
        btkNoticeClose: function (){
            uiRefBTK.core.playSoundCore("Click");
            engine.call("btkUI-PopupNoticeOK");
            cvr("#btkUI-PopupNotice").hide();
        },
        btkToastDismiss: function (){
            uiRefBTK.core.playSoundCore("Click");
            cvr("#btkUI-AlertToastContainer").hide();
        },
        btkRowCollapse: function (e) {
            uiRefBTK.core.playSoundCore("Click");

            let rowTarget = e.currentTarget.getAttribute("data-row");

            btkCollapseCatFunc(rowTarget);
        }
    }
}
