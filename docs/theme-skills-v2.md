# Kỹ năng theo bộ mèo (Theme Set Skills)

> **Phiên bản:** 2.0 – Bản cân bằng đề xuất  
> **Trạng thái:** Đã cập nhật theo các quyết định thiết kế; chưa triển khai và vẫn cần playtest để chốt các giá trị kinh tế/vật lý.  
> **Phạm vi:** Hệ thống mở khóa kỹ năng theo bộ mèo, năng lượng trong ván, 7 kỹ năng, Gacha/mảnh sao, UI và kế hoạch kỹ thuật.

Ý tưởng cốt lõi: người chơi sưu tầm đủ 11 mèo của một chủ đề để mở khóa vĩnh viễn kỹ năng của chủ đề đó. Để sử dụng kỹ năng trong ván, người chơi phải mặc đủ cả 11 vị trí bằng mèo thuộc cùng chủ đề. Chủ đề mặc định (Cổ điển) không có kỹ năng.

Các thông số được chia thành hai nhóm:
- **Đã thống nhất:** các quy tắc người dùng đã xác nhận, ví dụ năng lượng +6 mỗi merge, bắt đầu 30%, điều kiện mặc đủ 11/11.
- **Đề xuất cần playtest:** các giá trị được đề xuất để bắt đầu cân bằng, ví dụ giá đổi mảnh sao theo độ hiếm và lực hút của Cái ôm.

---

## 1. Tóm tắt hệ thống

| Bước | Điều kiện | Kết quả |
|---|---|---|
| **Mở khóa** | Sở hữu đủ **11/11** mèo của một chủ đề thông qua Gacha hoặc đổi mảnh sao | Kỹ năng của chủ đề được mở khóa vĩnh viễn |
| **Kích hoạt** | Đã mở khóa và đang mặc đủ **11 vị trí** bằng đúng chủ đề đó khi bắt đầu/tiếp tục ván | Trong ván xuất hiện nút kỹ năng; năng lượng được nạp bằng cách merge mèo |
| **Chủ đề Cổ điển** | – | Không có kỹ năng |
| **Mặc lẫn nhiều chủ đề** | – | Không có kỹ năng; người chơi vẫn được tự do phối đồ |

Luật này tạo ra một lựa chọn rõ ràng: người chơi có thể phối nhiều chủ đề theo sở thích hoặc mặc nguyên một bộ để sử dụng kỹ năng. Việc hoàn thành bộ sưu tập trở thành mục tiêu dài hạn thay vì chỉ thu thập vật phẩm riêng lẻ.

Có **7 chủ đề có kỹ năng**: Nét vẽ, Dễ thương, Chibi, Pixel, Nửa đêm, Kẹo ngọt và Hoàng kim.

### Mục tiêu cân bằng

- Một ván chơi thông thường có khoảng **35–50 lần merge**.
- Nguyên nhân thua phổ biến nhất là mèo chạm vạch đỏ.
- Kỹ năng nên là **hỗ trợ mạnh, có khả năng cứu một ván gần thua**, nhưng không được biến ván chơi thành bất bại.
- Mỗi kỹ năng cần có tình huống sử dụng riêng; không yêu cầu cả 7 kỹ năng mạnh ngang nhau trong mọi tình huống.
- Mục tiêu kiểm thử ban đầu: bộ có kỹ năng giúp điểm trung bình tăng khoảng **10–20%** so với ván không sử dụng kỹ năng. Đây là mục tiêu tham khảo, cần đo thực tế.

---

## 2. Luật mở khóa và kích hoạt

### 2.1. Mở khóa

- Tính theo số mèo **đang sở hữu** của chủ đề, không tính số mèo đã quay tổng cộng.
- Khi sở hữu đủ 11/11 mèo, kỹ năng được mở khóa vĩnh viễn.
- Mèo đổi bằng mảnh sao cũng được tính vào tiến độ sưu tập và điều kiện mở khóa bộ.
- Khi nhận được mèo cuối cùng của bộ từ Gacha hoặc đổi mảnh sao, hiển thị màn **“HOÀN THÀNH BỘ! Mở khóa kỹ năng: …”** với hiệu ứng riêng.
- Mở khóa kỹ năng không bị mất khi người chơi thay đổi trang bị sau đó.

### 2.2. Kích hoạt

- Điều kiện sử dụng: kỹ năng đã mở khóa **và** cả 11 vị trí mèo đang mặc đúng cùng một chủ đề.
- Kiểm tra điều kiện khi bắt đầu ván và khi tiếp tục ván đã lưu.
- Skin chỉ đổi được ở Menu, không đổi giữa ván.
- Nếu thiếu bất kỳ vị trí nào, kỹ năng không hoạt động. Ví dụ: mặc 10/11 mèo cùng bộ thì không được kích hoạt.
- Skin tab cần hiển thị số vị trí đang mặc và chỉ rõ vị trí còn thiếu.
- Nút **MẶC CẢ BỘ NÀY** là đường tắt để trang bị đủ 11 vị trí trong một thao tác.

### 2.3. Trạng thái hiển thị trên Skin và Sổ mèo

| Trạng thái | Hiển thị | Ví dụ |
|---|---|---|
| Chưa đủ bộ | Biểu tượng khóa mờ và tiến độ sưu tập | `Kỹ năng: Tẩy 🔒 7/11` |
| Đủ bộ, chưa mặc đủ | Biểu tượng sáng, thông báo đã mở khóa và số slot đang mặc | `Kỹ năng: Tẩy ✔ Mở khóa · Đang mặc 9/11` |
| Đủ bộ và mặc đủ | Biểu tượng sáng, trạng thái đang sử dụng | `Kỹ năng: Tẩy ✔ ĐANG DÙNG` |

### 2.4. Xem trước kỹ năng

- Collection Book/Sổ mèo luôn cho phép xem trước đầy đủ kỹ năng, kể cả khi người chơi chưa sở hữu đủ bộ.
- Với bộ chưa hoàn thành, hiển thị mô tả kỹ năng cùng tiến độ `x/11` và trạng thái khóa.
- Với bộ đã mở khóa nhưng chưa mặc đủ, hiển thị điều kiện còn thiếu để kích hoạt.
- Preview chỉ cung cấp thông tin, không cho phép sử dụng kỹ năng thử trong gameplay.

---

## 3. Năng lượng và cách sử dụng kỹ năng

### 3.1. Thông số năng lượng

| Thuộc tính | Giá trị v2 | Ghi chú |
|---|---:|---|
| Năng lượng tối đa | **100%** | Không vượt quá mức tối đa |
| Năng lượng đầu ván | **30%** | Áp dụng cho mỗi ván mới |
| Năng lượng mỗi lần merge | **+6%** | Mỗi merge hợp lệ trong gameplay thông thường |
| Chi phí sử dụng kỹ năng | **100%** | Dùng xong năng lượng trở về 0% |
| Khi đạt 100% | Dừng tích năng lượng | Merge tiếp không tạo năng lượng dư hoặc năng lượng dự trữ |
| Năng lượng khi Restart | **30%** | Reset như ván mới |
| Năng lượng khi Resume | Giữ nguyên | Đọc từ dữ liệu lưu của ván |

**Không áp dụng bonus năng lượng trong phiên bản đầu tiên:**
- Không cộng thêm năng lượng theo combo.
- Không cộng thêm năng lượng khi merge ra mèo cấp cao.
- Chỉ dùng mức cơ bản **+6% mỗi merge** để dễ dự đoán, kiểm thử và cân bằng.
- Có thể bổ sung bonus combo hoặc cấp mèo trong bản cập nhật sau nếu dữ liệu cho thấy người chơi nạp năng lượng quá chậm.

### 3.2. Quy tắc tính năng lượng

1. Mỗi lần merge hợp lệ trong gameplay thông thường cộng **6%** năng lượng.
2. Các lần merge dây chuyền phát sinh từ thao tác thả mèo thông thường vẫn được tính là merge hợp lệ.
3. Merge phát sinh trực tiếp do hiệu ứng kỹ năng không cộng năng lượng, tránh kỹ năng tự tạo năng lượng để tái sử dụng.
4. Trong thời gian kỹ năng đang có hiệu lực, các merge thông thường do người chơi tạo ra vẫn được cộng năng lượng. Không khóa toàn bộ việc nạp năng lượng chỉ vì một hiệu ứng có thời lượng đang chạy.
5. Khi năng lượng đạt 100%, các merge tiếp theo không cộng thêm và phần năng lượng vượt mức không được lưu.
6. Khi ván đã kết thúc, không tiếp tục cộng năng lượng.

> **Lưu ý triển khai:** cần phân biệt nguồn tạo merge (gameplay thông thường hay do skill trigger) trong event/hook merge. Không nên chỉ kiểm tra cờ `isSkillActive` để chặn mọi lần cộng năng lượng, vì như vậy người chơi có thể bị mất năng lượng từ các merge thông thường trong thời gian kỹ năng chạy.

### 3.3. Ước tính số lần sử dụng trong một ván

Với 30% năng lượng đầu ván và +6% mỗi merge:

- Từ 30% lên 100% cần `ceil((100 - 30) / 6) = 12` lần merge.
- Sau khi sử dụng kỹ năng, từ 0% lên 100% cần `ceil(100 / 6) = 17` lần merge.
- Nếu có 35 lần merge: về lý thuyết có thể nạp và dùng khoảng **2 lần**.
- Nếu có 50 lần merge: về lý thuyết có thể nạp và dùng khoảng **3 lần**.

Đây là ước tính khi người chơi sử dụng kỹ năng ngay khi đủ năng lượng. Số lần sử dụng thực tế còn phụ thuộc thời điểm người chơi kích hoạt kỹ năng, merge được tính hợp lệ và thời điểm ván kết thúc.

### 3.4. Nút kỹ năng

- Nút tròn có biểu tượng đầu mèo của chủ đề.
- Vòng tiến độ chạy quanh viền nút, thể hiện năng lượng hiện tại.
- Khi đủ 100%: nút sáng và có hiệu ứng nhấp nháy nhẹ.
- Khi chưa đủ năng lượng: bấm nút phát rung nhẹ và thông báo **“Chưa đủ năng lượng”**.
- Vị trí đề xuất: góc phải, bên dưới ô NEXT, trong vùng trống phía trên khung chơi. Cần kiểm tra trên màn hình thật để không che mèo đang thả.

### 3.5. Hai loại kỹ năng

1. **Chạm để chọn mục tiêu** – Tẩy, Lên cấp, Midas:
   - Bấm nút kỹ năng để vào chế độ chọn mục tiêu.
   - Màn chơi tối nhẹ; các mèo hợp lệ có thể được chọn.
   - Hiển thị gợi ý: **“Chạm một chú mèo”**.
   - Bấm ra ngoài hoặc bấm nút kỹ năng lần nữa để hủy miễn phí.
   - Chỉ trừ 100% năng lượng sau khi người chơi chọn một mục tiêu hợp lệ và kỹ năng thực sự được thực hiện.

2. **Dùng ngay** – Cái ôm, Thu nhỏ, Ánh trăng, Cơn sốt đường:
   - Bấm nút là kích hoạt ngay.
   - Hiển thị thời gian còn lại quanh nút đối với kỹ năng có thời lượng.

### 3.6. Tương tác với các phần khác

- **Pause/Cài đặt:** dừng mọi bộ đếm thời gian của kỹ năng theo thời gian trong game, không dùng giờ thực.
- **Hết ván:** khóa nút kỹ năng và dừng toàn bộ hiệu ứng đang chạy.
- **Restart/Ván mới:** năng lượng trở về 30%.
- **Tiếp tục ván dở:** khôi phục năng lượng đã lưu.
- **Hiệu ứng đang chạy khi lưu ván:** không lưu thời gian hiệu lực còn lại; khi tải lại, kỹ năng không tiếp tục hiệu ứng cũ.
- **Đang chọn mục tiêu:** không cho phép kéo thả mèo xuống bàn.
- **Bấm nút kỹ năng:** không được tính là thao tác thả mèo.

---

## 4. Bảy kỹ năng

### 4.1. Tổng quan

| Vai trò | Kỹ năng |
|---|---|
| Dọn bể | Tẩy (Nét vẽ), Midas (Hoàng kim) |
| Hỗ trợ merge | Cái ôm (Dễ thương), Lên cấp (Pixel) |
| Phòng thủ/cứu ván | Ánh trăng (Nửa đêm), Thu nhỏ (Chibi) |
| Tăng thưởng | Cơn sốt đường (Kẹo ngọt) |

| Chủ đề | Kỹ năng | Loại | Hiệu ứng | Thông số v2 |
|---|---|---|---|---|
| **Nét vẽ** | **Tẩy** | Chọn mục tiêu | Xóa một mèo bất kỳ khỏi bể | Mọi cấp; không điểm, không coin |
| **Dễ thương** | **Cái ôm** | Dùng ngay | Mèo cùng cấp ở gần nhau hút về phía nhau | 3 giây; bán kính 3 units |
| **Chibi** | **Thu nhỏ** | Dùng ngay | Thu nhỏ toàn bộ mèo để tạo thêm khoảng trống, sau đó phục hồi kích thước | Scale và collider ×0,85 trong 6 giây; phục hồi trong 1,5 giây |
| **Pixel** | **Lên cấp** | Chọn mục tiêu | Nâng mèo được chọn lên 1 cấp | Chỉ chọn cấp 0–8; kết quả tối đa cấp 9 |
| **Nửa đêm** | **Ánh trăng** | Dùng ngay | Tạm dừng bộ đếm nguy hiểm ở vạch đỏ | 8 giây; không reset bộ đếm |
| **Kẹo ngọt** | **Cơn sốt đường** | Dùng ngay | Tăng điểm và coin nhận từ merge | 10 giây; điểm ×2, coin merge ×1,5 |
| **Hoàng kim** | **Midas** | Chọn mục tiêu | Biến mèo thành vàng, bán và xóa khỏi bể | Chỉ chọn cấp 0–7; coin = `3 + 2 × cấp` (3–17) |

### 4.2. Tẩy – Nét vẽ

**Hiệu ứng:** chọn một chú mèo bất kỳ trên bàn để xóa khỏi bể.

- Có thể chọn mèo ở mọi cấp.
- Không cộng điểm, coin, thưởng khám phá hoặc phần thưởng merge.
- Không tạo sự kiện merge.
- Chỉ trừ năng lượng khi mục tiêu được chọn hợp lệ và xóa thành công.
- Có thể dùng để xóa mèo lớn ở đáy bể nhằm giải phóng không gian; đây là sức mạnh có chủ đích, được cân bằng bằng chi phí 100% năng lượng.

**Triển khai tham khảo:** dùng `Physics2D.OverlapPoint` để tìm đối tượng tại vị trí chạm, kiểm tra mục tiêu hợp lệ, sau đó hủy đối tượng và chạy hiệu ứng xóa/gạch.

**Rủi ro:** thấp. Cần bảo đảm xóa mèo không làm sai trạng thái vật lý, số lượng mèo, dữ liệu điểm và các tham chiếu đang giữ tới đối tượng.

### 4.3. Cái ôm – Dễ thương

**Hiệu ứng:** các mèo cùng cấp ở gần nhau hút về phía nhau để tăng khả năng chạm và merge.

- Thời lượng: **3 giây**.
- Bán kính ảnh hưởng: **3 units**.
- Chỉ hút các cặp mèo cùng cấp.
- Bắt đầu với lực hút nhỏ, có giới hạn lực tối đa.
- Không kéo mèo xuyên qua vật cản.
- Merge phát sinh trực tiếp do lực hút từ kỹ năng không cộng năng lượng.

**Triển khai tham khảo:** trong thời gian hiệu lực, duyệt các cặp mèo cùng cấp trong bán kính và áp dụng lực hút theo hướng nối hai tâm. Có thể giới hạn lực theo khối lượng để giảm hiện tượng mèo nhẹ bị kéo quá mạnh.

**Rủi ro:** lực hút quá lớn có thể làm bể rung, mèo dính vào nhau hoặc tạo chuỗi merge ngoài dự kiến. Cần bắt đầu từ lực nhỏ và đo trên thiết bị yếu. Không nên đặt lực cố định quá mạnh chỉ để đảm bảo chắc chắn xảy ra merge.

### 4.4. Thu nhỏ – Chibi

**Hiệu ứng:** toàn bộ mèo nhỏ lại để tạo thêm khoảng trống, sau đó từ từ trở về kích thước ban đầu.

- Scale hình ảnh và bán kính collider: **×0,85**.
- Giữ trạng thái thu nhỏ: **6 giây**.
- Phục hồi về kích thước gốc: **1,5 giây**.
- Phải lưu kích thước gốc của từng mèo và phục hồi đúng giá trị đó; không nhân scale ×0,85 lặp lại lên giá trị đã thu nhỏ.
- Collider phải được cập nhật tương ứng với kích thước hiển thị.
- Đề xuất tạm dừng bộ đếm nguy hiểm trong **1,5 giây phục hồi**, tránh việc mèo bị đẩy lên vạch đỏ trong lúc collider lớn trở lại.

**Triển khai tham khảo:** giảm scale và bán kính va chạm cùng lúc; sau 6 giây, tween dần về giá trị gốc trong 1,5 giây. Trong quá trình phục hồi, cần theo dõi chồng lấn collider và lực đẩy phát sinh.

**Rủi ro:** trung bình. Việc lớn lại có thể đẩy mèo trồi lên vạch đỏ, rung hoặc bật mạnh. Phải kiểm thử nhiều tình huống bể chật, đặc biệt khi nhiều mèo đang chồng sát nhau.

**Phương án thay thế nếu vật lý không ổn định:** kỹ năng **Nảy lên**, tạo xung lực nhỏ hướng lên cho mèo để xáo lại bàn chơi. Đây chỉ là phương án dự phòng; cấu hình v2 hiện tại vẫn chọn Thu nhỏ.

### 4.5. Lên cấp – Pixel

**Hiệu ứng:** chọn một mèo hợp lệ để nâng lên 1 cấp ngay lập tức.

- Chỉ cho phép chọn mèo cấp **0–8**.
- Kết quả cao nhất là mèo cấp **9**.
- Không thể dùng kỹ năng để tạo cấp 10 hoặc cấp 11.
- Cập nhật đầy đủ sprite, collider/radius và mass theo cấp mới.
- Được tính là mèo mới xuất hiện để cập nhật Sổ mèo/Codex và thưởng khám phá nếu cấp đó chưa từng được khám phá.
- Chỉ trừ năng lượng khi mục tiêu hợp lệ và thao tác nâng cấp thành công.

**Triển khai tham khảo:** dùng chung luồng cập nhật cấp mèo với hệ thống merge nếu có thể, nhưng cần phân biệt nguồn tạo cấp để không phát sinh điểm/coin merge không hợp lệ.

**Rủi ro:** thay đổi kích thước và khối lượng có thể đẩy các mèo xung quanh. Cần kiểm thử khi mục tiêu ở sát vạch đỏ hoặc bị kẹp giữa các mèo khác.

### 4.6. Ánh trăng – Nửa đêm

**Hiệu ứng:** tạm dừng bộ đếm nguy hiểm ở vạch đỏ trong **8 giây**.

- Tạm dừng `_dangerTimer` trong 8 giây.
- Hết thời gian, bộ đếm tiếp tục từ giá trị còn lại trước khi kỹ năng được kích hoạt.
- Không reset bộ đếm về giá trị ban đầu.
- Không xóa trạng thái nguy hiểm và không tự động đẩy mèo ra khỏi vạch đỏ.
- Không thể kích hoạt sau khi Game Over đã được xác nhận.
- Hiệu ứng hình ảnh đề xuất: nền tối xanh, vạch đỏ chuyển xanh, biểu tượng/vầng trăng ở góc.

**Vai trò cân bằng:** đây là kỹ năng phòng thủ trực tiếp, phù hợp với nguyên nhân thua phổ biến là mèo chạm vạch đỏ. Người chơi có thêm 8 giây để merge hoặc dọn bể, nhưng vẫn phải xử lý tình huống trước khi bộ đếm chạy hết.

**Rủi ro:** thấp nếu bộ đếm chỉ bị pause và tiếp tục đúng giá trị cũ. Cần bảo đảm nhiều nguyên nhân pause cùng lúc không làm bộ đếm chạy lại quá sớm hoặc bị pause vô thời hạn.

### 4.7. Cơn sốt đường – Kẹo ngọt

**Hiệu ứng trong 10 giây:**
- Điểm từ merge: **×2**.
- Coin từ merge: **×1,5**.

**Chỉ áp dụng cho:**
- Điểm phát sinh từ các lần merge hợp lệ trong thời gian kỹ năng có hiệu lực.
- Coin được trao trực tiếp từ các lần merge trong thời gian kỹ năng có hiệu lực.

**Không áp dụng cho:**
- Thưởng khám phá mèo mới/Codex.
- Hoàn tiền Gacha hoặc coin hoàn trả từ vật phẩm trùng.
- Thưởng nhiệm vụ, đăng nhập, quảng cáo hoặc nguồn coin khác không phải merge.
- Các loại thưởng không được xác định rõ là coin từ merge.

**Rủi ro:** tăng coin có thể làm lạm phát economy. Cần đo coin trung bình mỗi ván trước và sau khi có kỹ năng. Không để multiplier bị cộng dồn nhiều lần nếu có nhiều nguồn buff cùng loại; cần quy định thứ tự áp dụng multiplier thống nhất.

### 4.8. Midas – Hoàng kim

**Hiệu ứng:** chọn một mèo hợp lệ để biến thành vàng, bán lấy coin và xóa khỏi bể.

- Chỉ được chọn mèo cấp **0–7**.
- Coin nhận được: `3 + 2 × cấp`, tương ứng từ **3 đến 17 coin**.
- Không tạo điểm merge, không kích hoạt combo, không phát sinh thưởng khám phá.
- Khác Tẩy ở chỗ Midas trả coin nhưng giới hạn cấp mục tiêu và có giá bán thấp.
- Chỉ trừ năng lượng khi mục tiêu hợp lệ và bán thành công.

**Rủi ro kinh tế:** nếu cho bán mèo cấp cao với giá quá lớn, Midas có thể trở thành nguồn tạo coin quá dễ. Công thức `3 + 2 × cấp` là mức khởi điểm cần so sánh với thu nhập coin trung bình/ván và chi phí Gacha. Có thể chỉnh công thức trong `balance.json` sau playtest.

---

## 5. Cân bằng và đo đạc

### 5.1. Nguyên tắc

- Kỹ năng cần hỗ trợ mạnh và có thể cứu ván gần thua, nhưng không đảm bảo người chơi luôn sống sót.
- Không cần cả 7 kỹ năng có sức mạnh giống nhau; mỗi kỹ năng có thể mạnh trong một tình huống khác nhau.
- Không để một kỹ năng trở thành lựa chọn tối ưu trong hầu hết mọi tình huống.
- Ưu tiên cân bằng theo tình huống thua phổ biến: mèo chạm vạch đỏ.
- Tất cả thông số cần có thể chỉnh qua `balance.json`, hạn chế hard-code.

### 5.2. Chỉ số cần ghi nhận

| Chỉ số | Mục đích |
|---|---|
| `average_merges_per_run` | Xác minh giả định 35–50 merge/ván |
| `skill_uses_per_run` | Đánh giá số lần dùng kỹ năng thực tế |
| `average_score_with_skill` | Đo điểm trung bình khi sử dụng kỹ năng |
| `average_score_without_skill` | Làm mốc so sánh |
| `average_coins_per_run` | Đánh giá ảnh hưởng đến economy |
| `average_run_duration` | Phát hiện kỹ năng kéo dài ván quá mức |
| `red_line_death_rate` | Đánh giá khả năng kỹ năng giảm số lần thua do vạch đỏ |
| `skill_usage_rate_by_theme` | Tìm kỹ năng quá mạnh, quá yếu hoặc ít được sử dụng |
| `survival_time_after_skill` | Đo kỹ năng có thực sự giúp cứu ván hay chỉ trì hoãn thua |

### 5.3. Mục tiêu kiểm thử ban đầu

- Mức tăng điểm trung bình mục tiêu: khoảng **10–20%** khi sử dụng kỹ năng; đây là mục tiêu định hướng, không phải giới hạn cứng.
- Theo dõi riêng tỷ lệ thua do chạm vạch đỏ trước và sau khi dùng kỹ năng.
- Kiểm tra phân bố số lần dùng kỹ năng theo nhóm ván 35, 40, 45 và 50 merge.
- Theo dõi riêng người chơi mặc bộ kỹ năng và người chơi không mặc đủ bộ.
- Nếu người chơi thường xuyên kết thúc ván khi năng lượng còn gần 100%, có thể kỹ năng khó tiếp cận hoặc người chơi chưa nhận biết nút kỹ năng.
- Nếu người chơi dùng kỹ năng ngay khi đủ năng lượng nhưng vẫn thường thua ngay sau đó, cần xem lại hiệu quả cứu ván của từng kỹ năng.

---

## 6. Gacha và mảnh sao

### 6.1. Vấn đề độ khó hoàn thành bộ

Theo mô phỏng trong tài liệu gốc, Gacha hiện tại được giả định có:
- 70% Common/Thường.
- 25% Rare/Hiếm.
- 5% Legendary/Huyền thoại.
- Bảo hiểm Legendary mỗi 30 lượt.
- Các mèo cùng độ hiếm có xác suất ngang nhau.
- Giá quay: 150 coin/lượt.

Mô phỏng hoàn thành 11 mèo của một chủ đề với 4.000 lượt chạy:

| Kịch bản | Lượt quay trung bình | Trung vị | 90% người chơi cần tối đa |
|---|---:|---:|---:|
| **Hiện tại** (150 coin/lượt) | **464** | **380** | **835** |
| Tỉ lệ 60/30/10, bảo hiểm 30 | 326 | 290 | 529 |
| Bảo hiểm 20 lượt | 399 | 340 | 695 |

Với 464 lượt quay, tổng chi phí danh nghĩa khoảng **69.600 coin** (xấp xỉ 70.000 coin), chưa trừ phần coin hoàn lại khi trùng. Tài liệu gốc ước tính phần hoàn coin khoảng 17.000 coin; con số này cần xác minh lại bằng dữ liệu kinh tế và logic Gacha đang chạy thực tế.

> **Phạm vi mô phỏng:** số liệu trên là kết quả mô phỏng được ghi trong tài liệu gốc với giả định bảng rơi 3 độ hiếm như trên. Không tự động áp dụng cho bảng Gacha có thêm rarity Majesty hoặc thay đổi pool/rate.

### 6.2. Quy tắc mảnh sao v2

**Nguồn nhận mảnh sao:**
- Chỉ nhận mảnh sao khi Gacha trả về **mèo trùng**.
- Gacha ra mèo mới: không nhận mảnh sao.
- Mèo trùng: ngoài coin hoàn trả theo hệ thống hiện tại, nhận thêm mảnh sao.
- Không nhận mảnh sao từ background, frame hoặc các vật phẩm không phải mèo.

**Đổi mảnh sao:**
- Người chơi được chọn một mèo còn thiếu trong bất kỳ chủ đề nào.
- Có thể đổi cả mèo Legendary và Majesty.
- Không được đổi mèo người chơi đã sở hữu.
- Mèo đổi bằng mảnh sao được tính vào bộ sưu tập và điều kiện mở khóa kỹ năng.
- Giá đổi tăng theo độ hiếm để các mèo hiếm vẫn giữ giá trị Gacha.

### 6.3. Bảng giá đổi mảnh sao đề xuất

| Độ hiếm mèo | Giá đổi đề xuất |
|---|---:|
| Common/Thường | **20 mảnh** |
| Rare/Hiếm | **30 mảnh** |
| Legendary/Huyền thoại | **40 mảnh** |
| Majesty | **50 mảnh** |

**Lượng mảnh nhận đề xuất:** 1 mảnh sao cho mỗi mèo trùng, không phân biệt độ hiếm, ngoài phần coin hoàn trả hiện tại.

> **Trạng thái:** bảng giá 20/30/40/50 và lượng nhận 1 mảnh/mèo trùng là đề xuất cân bằng v2 để bắt đầu test, chưa phải kết quả đã được xác minh bằng mô phỏng. Có thể chỉnh trong config sau khi chạy mô phỏng và thu thập dữ liệu thực tế.

### 6.4. Kết quả mô phỏng cũ và giới hạn áp dụng

Tài liệu gốc có mô phỏng với giả định **mọi mèo còn thiếu đều đổi bằng cùng một mức giá mảnh sao**:

| Chi phí đổi đồng nhất | Lượt quay trung bình | Trung vị | 90% người chơi cần tối đa |
|---|---:|---:|---:|
| 10 mảnh | 101 | 101 | 120 |
| **20 mảnh** | **134** | **129** | **166** |
| 30 mảnh | 153 | 158 | 193 |
| 50 mảnh | 184 | 175 | 229 |

**Không sử dụng trực tiếp các con số trong bảng này làm dự báo cho cấu hình v2.** Bảng giá v2 thay đổi theo độ hiếm (20/30/40/50), vì vậy cần chạy lại mô phỏng theo từng pool/rate thực tế, đặc biệt với Legendary và Majesty.

Trước khi chạy mô phỏng mới, cần xác minh:
1. Gacha thực tế có những rarity nào, Majesty có phải rarity riêng hay là tên cấp mèo khác.
2. Tỉ lệ rơi từng rarity và cách pity hoạt động.
3. Số lượng mèo trong mỗi rarity thuộc từng chủ đề.
4. Mèo trùng ở mỗi rarity có cùng lượng mảnh sao hay không; cấu hình v2 hiện đề xuất 1 mảnh cho mọi mèo trùng.
5. Quy tắc coin hoàn trả cho vật phẩm trùng.

### 6.5. Các đòn bẩy economy khác

Có thể cân nhắc sau khi đo dữ liệu, không cần áp dụng đồng thời ngay từ đầu:
- Giảm giá quay từ 150 xuống 100 coin/lượt.
- Tăng coin từ merge mèo.
- Thêm thưởng đăng nhập hoặc nhiệm vụ hằng ngày.
- Điều chỉnh tỉ lệ Legendary nếu đây là điểm nghẽn lớn.
- Thêm pity theo chủ đề: sau N lượt, bảo đảm nhận một mèo còn thiếu của chủ đề đang theo dõi.

---

## 7. Giao diện

### 7.1. Trong game

Bố cục tham khảo:

```text
┌──────────────────────────────┐
│ 1520              ⚙      NEXT│
│ BEST 3606               (🐱) │
│ (🪙 1891)               ╭───╮│
│                         │ ✨││  ← nút kỹ năng (vòng năng lượng quanh viền)
│                         ╰───╯│
│   ┌────────────────────┐     │
│   │                    │     │
│   │     khung chơi     │     │
```

- Nút kỹ năng đặt ở góc phải, dưới NEXT; cần xác minh trên nhiều tỉ lệ màn hình.
- Khi chưa đủ năng lượng: nút thể hiện trạng thái chưa sẵn sàng.
- Khi đủ năng lượng: viền sáng và có hiệu ứng nhấp nháy nhẹ.
- Đang chọn mục tiêu: nền tối nhẹ, hiển thị dòng **“Chạm một chú mèo để dùng kỹ năng”** và nút **HUỶ**.
- Khi kỹ năng có thời lượng đang chạy: hiển thị countdown quanh nút và hiệu ứng theo chủ đề.
- Không để nút kỹ năng che vị trí thả mèo hoặc các thông tin gameplay quan trọng.

### 7.2. Skin tab

- Thêm khu vực **“Kỹ năng bộ”** ở phần thông tin chủ đề.
- Hiển thị tên, mô tả và trạng thái: khóa / đã mở khóa / đang sử dụng.
- Hiển thị tiến độ sở hữu `x/11`.
- Khi đã mở khóa nhưng chưa mặc đủ 11 vị trí, hiển thị số slot đang mặc và hướng dẫn trang bị phần còn thiếu.
- Nút **MẶC CẢ BỘ NÀY** hiển thị tiến độ và trạng thái có thể dùng kỹ năng sau khi trang bị.

### 7.3. Sổ mèo / Collection Book

- Mỗi trang chủ đề có thẻ kỹ năng ở cuối trang.
- Hiển thị biểu tượng, tên, mô tả ngắn và tiến độ `x/11`.
- Cho phép xem trước toàn bộ kỹ năng của bộ chưa hoàn thành.
- Bộ đã hoàn thành hiển thị đã mở khóa; nếu chưa mặc đủ, giải thích điều kiện còn thiếu.

### 7.4. Gacha

- Khi một lượt quay giúp người chơi hoàn thành 11/11 mèo của bộ, hiển thị màn **“HOÀN THÀNH BỘ”** trước khi trở về màn kết quả.
- Với mèo thuộc bộ chưa đủ, thẻ kết quả có thể hiển thị tiến độ, ví dụ `Bộ Pixel 8/11`.
- Khi quay trùng mèo, hiển thị coin hoàn trả và số mảnh sao nhận được.
- Trong màn đổi mảnh sao, hiển thị rõ tên mèo, chủ đề, độ hiếm, giá đổi và số mảnh hiện có.
- Nút đổi bị khóa khi không đủ mảnh hoặc mèo đã sở hữu.

---

## 8. Thay đổi kỹ thuật dự kiến

| Phần | Việc cần làm |
|---|---|
| `Cosmetics` (Data) | Thêm định nghĩa kỹ năng theo chủ đề: id, tên EN/VI, mô tả EN/VI, loại kỹ năng, thông số. Có `ActiveSkill` (đủ bộ + mặc đủ 11 slot), `IsSetComplete(theme)`, `OwnedInTheme(theme)`. |
| `PlayerData` | Lưu cờ đã xem thông báo mở khóa bộ; lưu số mảnh sao; lưu dữ liệu cần thiết cho tiến độ đổi mèo. |
| `RunSnapshot` | Lưu `energy` hiện tại. Không lưu thời gian hiệu lực của kỹ năng đang chạy theo quy tắc v2. |
| `Balance` / `balance.json` | Lưu năng lượng, thời lượng, hệ số, cấp mục tiêu tối đa, công thức coin Midas, giá đổi mảnh sao và lượng mảnh nhận từ mèo trùng. |
| `GameManager` | Hook vào event merge để cộng năng lượng; phân biệt merge gameplay thông thường và merge do kỹ năng tạo ra; hỗ trợ pause danger timer, nhân điểm/coin, xóa/nâng cấp mèo, lực hút và thu nhỏ collider. |
| `SkillSystem` (mới) | Quản lý năng lượng, trạng thái chọn mục tiêu, kiểm tra điều kiện dùng kỹ năng và các hiệu ứng đang chạy. Mỗi kỹ năng nên tách thành lớp/module nhỏ. |
| `GameUI` | Nút kỹ năng, vòng năng lượng, chế độ chọn mục tiêu, hủy lựa chọn, countdown và thông báo chưa đủ năng lượng. |
| `SkinPage` / `CodexScreen` / `GachaPage` | Thẻ kỹ năng, preview, trạng thái mở khóa, tiến độ 11/11, màn hoàn thành bộ và UI đổi mảnh sao. |
| `Loc` | Khoảng 7 bộ tên + mô tả kỹ năng, cùng các chuỗi giao diện EN/VI. |
| `AudioManager` / `Fx` | Âm thanh và hiệu ứng riêng cho từng kỹ năng. Có thể tạo hiệu ứng bằng code nếu phù hợp. |
| Cheat/Debug | Thêm cheat mở khóa mọi kỹ năng, mặc đủ bộ, nạp đầy năng lượng và cấp mảnh sao để test nhanh. |

### 8.1. Cấu hình `balance.json` tham khảo

Các key dưới đây là cấu trúc tham khảo. Cần điều chỉnh tên key theo convention thực tế của project.

```json
{
  "themeSkill": {
    "maxEnergy": 100,
    "startingEnergy": 30,
    "energyPerMerge": 6,
    "skillCost": 100,
    "allowEnergyOverflow": false,
    "countNormalMergesDuringActiveSkill": true,
    "countSkillTriggeredMerges": false,
    "shardRewardPerDuplicateCat": 1,
    "shardExchangeCost": {
      "common": 20,
      "rare": 30,
      "legendary": 40,
      "majesty": 50
    },
    "skills": {
      "hug": {
        "duration": 3,
        "radius": 3
      },
      "shrink": {
        "scaleMultiplier": 0.85,
        "duration": 6,
        "restoreDuration": 1.5,
        "pauseDangerTimerDuringRestore": true
      },
      "moonlight": {
        "duration": 8
      },
      "sugarRush": {
        "duration": 10,
        "scoreMultiplier": 2,
        "mergeCoinMultiplier": 1.5
      },
      "midas": {
        "baseCoin": 3,
        "coinPerLevel": 2,
        "maxTargetLevel": 7
      },
      "levelUp": {
        "minTargetLevel": 0,
        "maxTargetLevel": 8,
        "maxResultLevel": 9
      }
    }
  }
}
```

### 8.2. Thứ tự triển khai đề xuất

1. **Framework:** luật mở khóa/kích hoạt, trạng thái trên Skin/Sổ mèo, nút kỹ năng, năng lượng, lưu năng lượng và cheat. Làm kèm hai kỹ năng dễ kiểm soát: **Cơn sốt đường** và **Ánh trăng**.
2. **Kỹ năng chọn mục tiêu:** Tẩy, Lên cấp, Midas.
3. **Kỹ năng vật lý:** Cái ôm và Thu nhỏ. Đây là nhóm cần nhiều thời gian kiểm thử nhất.
4. **Kinh tế Gacha:** mảnh sao, đổi mèo, màn hoàn thành bộ và mô phỏng bảng giá theo rarity.
5. **Hoàn thiện:** hiệu ứng, âm thanh, localization và kiểm thử trên thiết bị thật.

---

## 9. Rủi ro tổng hợp

| Rủi ro | Mức | Cách giảm |
|---|---|---|
| Đủ bộ quá khó, người chơi bỏ cuộc | Cao | Mảnh sao; mô phỏng lại chi phí theo rarity; điều chỉnh rate/giá nếu cần |
| Kỹ năng vật lý gây lỗi (rung, mèo văng, collider chồng lấn) | Trung bình | Làm sau cùng; bắt đầu với lực nhỏ; test trên thiết bị yếu và các tình huống bể chật |
| Kỹ năng quá mạnh làm ván quá dễ | Trung bình | Theo dõi điểm, tỷ lệ sống sót và thời lượng ván; chỉnh trong `balance.json` |
| Kỹ năng tăng coin gây lạm phát | Trung bình | Sugar Rush chỉ nhân coin merge; theo dõi coin trung bình/ván; điều chỉnh Midas |
| Người chơi không hiểu vì sao không có kỹ năng | Trung bình | Hiển thị rõ tiến độ sở hữu và số slot đang mặc ở Skin/Sổ mèo |
| Giao diện gameplay quá phức tạp | Thấp | Một nút kỹ năng; chỉ hiển thị khi điều kiện bộ được đáp ứng |
| Năng lượng được cộng sai từ merge dây chuyền hoặc skill | Trung bình | Phân loại nguồn tạo merge trong event; test riêng merge thông thường và merge do skill |
| Bộ đếm nguy hiểm bị pause sai khi có nhiều hiệu ứng | Trung bình | Quản lý các lý do pause độc lập; chỉ resume khi không còn lý do pause nào hoạt động |
| Mô phỏng mảnh sao không khớp bảng giá mới | Cao | Không dùng lại kết quả đổi đồng giá; chạy lại mô phỏng theo rarity và pool thật |

---

## 10. Các quyết định thiết kế đã xác nhận

| # | Nội dung | Quyết định |
|---:|---|---|
| 1 | Cách nạp kỹ năng | Nạp bằng merge mèo |
| 2 | Mức nạp năng lượng | +6% mỗi merge; bắt đầu ván ở 30% |
| 3 | Điều kiện mặc bộ | Phải mặc đủ 11/11 vị trí cùng một chủ đề |
| 4 | Mảnh sao | Có hệ thống mảnh sao; dùng để đổi mèo còn thiếu |
| 5 | Kỹ năng Chibi | Giữ Thu nhỏ, scale/collider ×0,85 trong 6 giây |
| 6 | Kẹo ngọt | Điểm ×2 và coin từ merge ×1,5 |
| 7 | Pixel | Chọn mèo cấp 0–8, kết quả tối đa cấp 9 |
| 8 | Preview | Collection Book hiển thị preview đầy đủ kỹ năng |
| 9 | Lưu ván | Giữ năng lượng khi tiếp tục ván đã gián đoạn |
| 10 | Tên kỹ năng | Giữ các tên hiện tại |
| 11 | Số merge trung bình | Khoảng 35–50 lần merge/ván |
| 12 | Nguyên nhân thua phổ biến | Mèo chạm vạch đỏ |
| 13 | Mức độ mạnh | Hỗ trợ mạnh, có thể cứu ván gần thua |
| 14 | Năng lượng khi đạt tối đa | Dừng ở 100%, không lưu phần dư |
| 15 | Nguồn mảnh sao | Chỉ nhận khi Gacha ra mèo trùng |

### 10.1. Các đề xuất v2 chưa được xác minh bằng playtest

- Giá đổi mảnh sao theo rarity: Common 20, Rare 30, Legendary 40, Majesty 50.
- Mỗi mèo trùng nhận 1 mảnh sao, không phân biệt rarity.
- Merge thông thường vẫn cộng năng lượng khi một kỹ năng có thời lượng đang chạy; merge do hiệu ứng kỹ năng trực tiếp tạo ra không cộng năng lượng.
- Không cộng bonus năng lượng theo combo hoặc cấp mèo trong phiên bản đầu tiên.
- Tạm dừng bộ đếm nguy hiểm trong 1,5 giây phục hồi collider của Thu nhỏ.
- Lực hút của Cái ôm bắt đầu nhỏ và có giới hạn tối đa.
- Công thức coin Midas `3 + 2 × cấp` cần được so sánh với economy thực tế.

---

## 11. Checklist playtest

### 11.1. Năng lượng và điều kiện kỹ năng

- [ ] Ván mới bắt đầu ở đúng 30% năng lượng.
- [ ] Mỗi merge hợp lệ cộng đúng 6%.
- [ ] Merge dây chuyền từ thao tác gameplay thông thường được tính đúng.
- [ ] Merge phát sinh trực tiếp từ kỹ năng không cộng năng lượng.
- [ ] Merge thông thường trong lúc kỹ năng có thời lượng đang chạy vẫn cộng năng lượng.
- [ ] Năng lượng dừng đúng ở 100%, không có phần dư.
- [ ] Sử dụng kỹ năng trừ đúng 100% và năng lượng về 0%.
- [ ] Chỉ mặc đủ 11/11 mèo cùng Theme mới thấy/được dùng kỹ năng.
- [ ] Hủy chế độ chọn mục tiêu không mất năng lượng.
- [ ] Không thể thả mèo khi đang ở chế độ chọn mục tiêu.
- [ ] Năng lượng được giữ khi load ván đã lưu.
- [ ] Hiệu ứng đang chạy không được khôi phục sau khi load ván đã lưu.
- [ ] Restart reset năng lượng về 30%.
- [ ] Game Over khóa nút và dừng hiệu ứng.

### 11.2. Kiểm thử riêng từng kỹ năng

- [ ] **Tẩy:** xóa được mèo ở mọi cấp; không nhận điểm/coin/thưởng khám phá.
- [ ] **Cái ôm:** chỉ hút mèo cùng cấp trong bán kính; lực không làm bể rung hoặc đẩy mèo xuyên vật cản.
- [ ] **Thu nhỏ:** hình ảnh và collider cùng giảm ×0,85; phục hồi đúng scale/radius gốc; không bị co nhỏ lặp lại.
- [ ] **Thu nhỏ:** bộ đếm nguy hiểm được pause trong 1,5 giây phục hồi theo cấu hình.
- [ ] **Lên cấp:** chỉ chọn cấp 0–8; không tạo cấp trên 9; sprite/radius/mass được cập nhật.
- [ ] **Lên cấp:** cập nhật Codex và thưởng khám phá đúng quy tắc.
- [ ] **Ánh trăng:** pause bộ đếm 8 giây rồi tiếp tục đúng giá trị cũ; không thể kích hoạt sau Game Over.
- [ ] **Cơn sốt đường:** điểm merge ×2 và coin merge ×1,5; không nhân các nguồn thưởng khác.
- [ ] **Midas:** chỉ chọn cấp 0–7; nhận đúng `3 + 2 × cấp`; không tạo điểm merge hoặc thưởng khám phá.

### 11.3. Gacha và mảnh sao

- [ ] Mèo mới không nhận mảnh sao.
- [ ] Mèo trùng nhận đúng lượng mảnh sao cấu hình.
- [ ] Vật phẩm background/frame trùng không nhận mảnh sao.
- [ ] Có thể đổi mèo còn thiếu ở bất kỳ Theme nào.
- [ ] Không thể đổi mèo đã sở hữu.
- [ ] Giá đổi tăng đúng theo rarity.
- [ ] Đổi được Legendary và Majesty khi đủ mảnh.
- [ ] Mèo đổi bằng mảnh sao cập nhật tiến độ bộ và mở khóa kỹ năng đúng cách.
- [ ] Màn hoàn thành bộ xuất hiện đúng một lần theo quy tắc thông báo.

### 11.4. Kiểm thử cân bằng

- [ ] Thu thập số merge trung bình/ván.
- [ ] Thu thập số lần dùng kỹ năng/ván.
- [ ] So sánh điểm trung bình có và không có kỹ năng.
- [ ] So sánh coin trung bình/ván trước và sau khi có Sugar Rush/Midas.
- [ ] So sánh tỷ lệ thua do chạm vạch đỏ.
- [ ] Kiểm tra các ván có 35, 40, 45 và 50 lần merge.
- [ ] Chạy mô phỏng Gacha mới với giá đổi mảnh sao theo rarity.

---

## 12. Ghi chú trước khi triển khai

1. Dùng cấu hình năng lượng đơn giản `30% + 6%/merge`, không bonus combo/cấp mèo trong phiên bản đầu tiên.
2. Ưu tiên hoàn thiện khung điều kiện 11/11, UI và hệ thống lưu năng lượng trước khi làm kỹ năng vật lý.
3. Ánh trăng và Thu nhỏ cần xử lý bộ đếm nguy hiểm cẩn thận vì nguyên nhân thua phổ biến là mèo chạm vạch đỏ.
4. Giữ công thức Midas và giá đổi mảnh sao trong config để dễ điều chỉnh.
5. Không đưa ra kết luận về số lượt Gacha cần thiết với bảng giá mảnh sao v2 cho tới khi chạy lại mô phỏng theo đúng rarity, pool và pity đang dùng trong game.

**Kết luận:** Bản v2 đã chốt được khung luật Theme Set Skills và các lựa chọn gameplay chính. Những điểm cần dữ liệu thực tế nhất trước khi phát hành là giá mảnh sao theo độ hiếm, thu nhập coin từ Midas/Sugar Rush và hiệu quả cứu ván của từng kỹ năng.
