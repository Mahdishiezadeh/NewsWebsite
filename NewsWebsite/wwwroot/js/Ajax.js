$(function () {
    var placeholder = $("#modal-placeholder");
    $(document).on('click','button[data-toggle="ajax-modal"]',function () {
        var url = $(this).data('url');
        $.ajax({
            url: url,
            beforeSend: function () { ShowLoading(); },
            complete: function () { $("body").preloader('remove'); },
            error: function () {
                ShowSweetErrorAlert();
            }
        }).done(function (result) {
            placeholder.html(result);
            placeholder.find('.modal').modal('show');
        });
    });

    placeholder.on('click', 'button[data-save="modal"]', function () {
        ShowLoading();
        var form = $(this).parents(".modal").find('form');
        var actionUrl = form.attr('action');

        if (form.length == 0) {
            form = $(".card-body").find('form');
            actionUrl = form.attr('action') + '/' + $(".modal").attr('id');
        }
      
        var dataToSend = new FormData(form.get(0));

        $.ajax({
            url: actionUrl, type: "post", data: dataToSend, processData: false, contentType: false, error: function () {
                ShowSweetErrorAlert();
            }}).done(function (data) {
                var newBody = $(".modal-body", data);
                var newFooter = $(".modal-footer", data);
                placeholder.find(".modal-body").replaceWith(newBody);
                placeholder.find(".modal-footer").replaceWith(newFooter);

            var IsValid = newBody.find("input[name='IsValid']").val() === "True";
            if (IsValid) {
                $.ajax({ url: '/Admin/Base/Notification', error: function () { ShowSweetErrorAlert(); } }).done(function (notification) {
                    ShowSweetSuccessAlert(notification)
                });

                $table.bootstrapTable('refresh')
                placeholder.find(".modal").modal('hide');
            }
        });

        $("body").preloader('remove');
    });
});

function ShowSweetErrorAlert() {
    Swal.fire({
        type: 'error',
        title: 'خطایی رخ داده است !!!',
        text: 'لطفا تا برطرف شدن خطا شکیبا باشید.',
        confirmButtonText: 'بستن'
    });
}

function ShowLoading() {
    $("body").preloader({ text: 'لطفا صبر کنید ...' });
}

function ShowSweetSuccessAlert(message) {
    Swal.fire({
        position: 'top-middle',
        type: 'success',
        title: message,
        confirmButtonText: 'بستن',
    })
}
///روی هر تگ آ در پارشیال ویو 
///      Views/Home/_MustViewedNews
///کلیک شود این فانکشن اجرا میشود
$(document).on('click', 'a[data-toggle="tab"]', function () {
    ///مقدار دیتا یو آر ال تگ آ را دریافت میکنیم 
    var url = $(this).data('url');
    ///آیدی آن را نیز دریافت میکنیم 
    var id = $(this).attr('id');
    ///این آدی ها را نیز در ویو ایندکس واقع در پوشه هوم قرار دادیم
    var contentDivId = "#MostViewedNewsDiv";
    ///پوشه آیکونز را به دابلیو دابلیو دابلیو روت اضافه میکنیم تا این کد فعالیت کند 
    var loadingDivId = "#nav-mostViewedNews";
    ///این بخش برای پیاده سازی پربحث ترین اخبار است
    ///اگر تگ آ ولیو ماست تاک رو داشته باشه 
    if($(this).hasClass("most-talk"))
    {
       
        /// کانتنت دیو آیدی در ویو ایندکس هوم پیج مقدار دهی  شده است 
        contentDivId = "#MostTalkNewsDiv";
        ///لودینگ دیو آیدی را نیز از طریق پارشیال ویو ماست تاک نیوز مقداردهی کردیم
        loadingDivId="#nav-mostTalkNews";
    }
    ///درخواست ایجکس را ارسال میکنیم 
    $.ajax({
        ///یو آر ال و بی فور سند را مقداردهی کردیم
        url: url,
        beforeSend: function () {$(loadingDivId).html("<p class='text-center mb-5 mt-3'><span style='font-size:18px;font-family: Vazir_Medium;'>در حال بارگزاری اطلاعات خبر </span><img src='/icons/LoaderIcon.gif'/></p>")},
        error: function () {
           ShowSweetErrorAlert();
        }
        ///زمانی که درخواست ایجکس با موفقیت انجام شد 
    }).done(function (result) {
        ///محتویات تگ های دیو داخل ویو ایندکس در پوشه هوم را آپدیت میکنیم
        $(contentDivId).html(result);
        ///و در آخر کلاس اکتیو را از تگ آ حذف میکنیم 
        $(contentDivId + " a").removeClass("active");
        ///و کلاس اکتیو را به آن تگ آ میدهیم که روی آن کلیک شده است 
       $("#"+id).addClass("active");
    });
});


$(document).on('click', 'button[data-save="Ajax"]', function () {
    var form = $(".newsletter-widget").find('form');
    var actionUrl = form.attr('action');
    var dataToSend = new FormData(form.get(0));

    $.ajax({
        url: actionUrl, type: "post", data: dataToSend, processData: false, contentType: false, error: function () {
            ShowSweetErrorAlert();
        }
    }).done(function (data) {
        var newForm = $("form", data);
        $(".newsletter-widget").find("form").replaceWith(newForm);
        var IsValid = newForm.find("input[name='IsValid']").val() === "True";
        if (IsValid) {
            $.ajax({ url: '/Admin/Base/Notification', error: function () { ShowSweetErrorAlert(); } }).done(function (notification) {
                ShowSweetSuccessAlert(notification)
            });
        }
    });
});






