// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
// --- LOGIC XỬ LÝ SO SÁNH SẢN PHẨM (CLIENT-SIDE) ---
let compareList = JSON.parse(localStorage.getItem('compareList')) || [];

// 1. Hàm thêm sản phẩm vào danh sách so sánh
function addToCompare(id, name, img) {
    // Check nếu trùng ID
    if (compareList.some(x => x.id === id)) return;

    // Check nếu đã có 3 món
    if (compareList.length >= 3) {
        alert("Ní ơi, so sánh tối đa 3 món thôi nha!");
        return;
    }

    // Thêm vào danh sách tạm
    compareList.push({ id, name, img });
    updateLocalStorageAndRender();
}

// 2. Hàm xóa 1 sản phẩm khỏi danh sách
function removeFromCompare(id) {
    compareList = compareList.filter(x => x.id !== id);
    updateLocalStorageAndRender();
}

// 3. Hàm xóa tất cả
function clearAllCompare() {
    compareList = [];
    updateLocalStorageAndRender();
}

// 4. Hàm cập nhật LocalStorage và vẽ lại giao diện
function updateLocalStorageAndRender() {
    localStorage.setItem('compareList', JSON.stringify(compareList));
    renderStickyBar();
}

// 5. Hàm vẽ lại thanh Sticky Compare Bar
function renderStickyBar() {
    const bar = $("#sticky-compare-bar");
    const list = $("#compare-list-items");
    const inputHidden = $("#idsCompareHidden");
    list.empty();

    if (compareList.length > 0) {
        bar.slideDown(); // Hiện thanh bar mượt mà

        // Tạo 3 ô Slot (kể cả ô trống)
        for (let i = 0; i < 3; i++) {
            if (compareList[i]) {
                const item = compareList[i];
                list.append(`
                    <div class="compare-item-slot border-solid">
                        <span class="btn-remove-compare" onclick="removeFromCompare('${item.id}')">x</span>
                        <img src="${item.img}" title="${item.name}" />
                    </div>
                `);
            } else {
                // Ô trống nếu chưa chọn đủ 3 món
                list.append(`<div class="compare-item-slot text-muted small">Món ${i + 1}</div>`);
            }
        }

        // Cập nhật giá trị ID vào input hidden để gửi sang Controller
        const ids = compareList.map(x => x.id).join(',');
        inputHidden.val(ids);

    } else {
        bar.slideUp(); // Hết hàng thì ẩn đi
    }
}

// Chạy hàm vẽ lại khi trang vừa load để giữ dữ liệu cũ
$(document).ready(function () {
    renderStickyBar();
});

