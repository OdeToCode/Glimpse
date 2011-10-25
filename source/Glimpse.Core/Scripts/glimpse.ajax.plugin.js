﻿var glimpseAjaxPlugin = (function ($, glimpse) {

/*(im port:glimpse.ajax.spy.js|2)*/

    var ConnectionNotice = function (scope) {
        var that = (this === window) ? {} : this;
        that.scope = scope;
        that.text = scope.find('span');
        return that;
    };
    ConnectionNotice.prototype = {
        connected : false, 
        prePoll : function () {
            var that = this;
            if (!that.connected) { 
                that.text.text('Connecting...'); 
                that.scope.removeClass('gconnect').removeClass('loading').addClass('gdisconnect');
            }
        },
        complete : function (textStatus) {
            var that = this;
            if (textStatus != "Success") {
                that.connected = false;
                that.text.text('Error...');
                that.scope.removeClass('gconnect').removeClass('loading').addClass('gdisconnect');
            }
            else {
                that.connected = true;
                that.text.text('Connected...');
                that.scope.removeClass('gdisconnect').removeClass('loading').addClass('gconnect');
            }
        }
    };


    var //Support
        isActive = false, 
        resultCount = 0;
        notice = undefined,
        wireListener = function () { 
            //glimpse.pubsub.subscribe('hook.render.engine', function (topic, payload) { registerEngine(payload) }); 

            glimpse.pubsub.subscribe('action.plugin.deactive', function (topic, payload) { if (payload == 'Ajax') { deactive(); } }); 
            glimpse.pubsub.subscribe('action.plugin.active', function (topic, payload) {  if (payload == 'Ajax') { active(); } }); 
        },
        alterCurrent = function () {
            var data = glimpse.data.current().data,
                metadata = glimpse.data.currentMetadata().plugins;

            data.Ajax = { name : 'Ajax', data : 'No requests currently detected...', isPermanent : true };
            metadata.Ajax = { helpUrl : 'http://getglimpse.com/Help/Plugin/Ajax' };
        },
        active = function () {
            isActive = true;
            getData(); 
        },
        deactive = function () {
            isActive = false; 
        },


        getData = function () { 
            if (!isActive) { return; }

            var panel = glimpse.elements.findPanel('Ajax'); 
            if (!notice) {
                panel.html('<div class="glimpse-target"></div><div class="glimpse-clear"><a href="#">Clear</a></div><div class="glimpse-notice gdisconnect"><div class="icon"></div><span>Disconnected...</span></div>');
                notice = new ConnectionNotice(panel.find('.glimpse-notice'));
            }

            notice.prePoll();
            $.ajax({
                url: glimpsePath + 'Ajax',
                data: { 'glimpseId' : glimpseData.requestId },
                type: 'GET',
                contentType: 'application/json',
                complete : function(jqXHR, textStatus) {
                    notice.complete(textStatus);
                    setTimeout(getData, 1000);
                },
                success: function (result) {
                    if (resultCount != result.length)
                        processData(result, panel);
                    resultCount = result.length; 
                }
            });
        },
        processData = function (result, panel) { 
            panel.find('.glimpse-target').html(glimpse.render.build(result))
        },

        //Main 
        init = function () {
            wireListener();
            alterCurrent();
        };

    init();
}($Glimpse, glimpse));