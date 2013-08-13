﻿'use strict';
define(['core/app/detourService'], function (detour) {
    detour.registerController([
        'CreateManyToManyCtrl',
        ['$scope', 'logger', '$detour', '$stateParams',
            function ($scope, logger, $detour, $stateParams) {
                
                $scope.showPrimaryList = true;
                $scope.showRelatedList = true;
                
                $scope.save = function () {
                    $("input.primary-entity").prop('disabled', false);
                    var form = $('#manytomany-form');
                    $.ajax({
                        url: form.attr('action'),
                        type: form.attr('method'),
                        data: form.serializeArray(),
                        success: function () {
                            logger.success('success');
                            $("input.primary-entity").prop('disabled', true);
                        },
                        error: function (result) {
                            logger.error('Failed:\n' + result.responseText);
                            $("input.primary-entity").prop('disabled', true);
                        }
                    });
                };

                $scope.exit = function () {
                    $detour.transitionTo('EntityDetail.Relationships', { Id: $stateParams.EntityName });
                };
            }]
    ]);
});
