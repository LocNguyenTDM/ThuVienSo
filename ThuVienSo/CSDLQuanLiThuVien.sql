

IF DB_ID(N'QuanLyThuVien') IS NULL
BEGIN
    CREATE DATABASE QuanLyThuVien;
END;
GO

USE QuanLyThuVien;
GO

/* ============================================================
   1. TAO CAC BANG GOC (NEU CHUA TON TAI)
   ============================================================ */

IF OBJECT_ID(N'dbo.nguoi_dung', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.nguoi_dung (
        id INT NOT NULL PRIMARY KEY,
        ho_ten NVARCHAR(100) NULL,
        email VARCHAR(100) NULL,
        mat_khau VARCHAR(255) NULL,
        so_dien_thoai VARCHAR(15) NULL,
        vai_tro NVARCHAR(20) NULL,
        trang_thai NVARCHAR(20) NULL
    );
END;
GO

IF OBJECT_ID(N'dbo.the_loai', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.the_loai (
        id INT NOT NULL PRIMARY KEY,
        ten_the_loai NVARCHAR(100) NULL,
        mo_ta NVARCHAR(255) NULL
    );
END;
GO

IF OBJECT_ID(N'dbo.tac_gia', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tac_gia (
        id INT NOT NULL PRIMARY KEY,
        ten_tac_gia NVARCHAR(100) NULL,
        quoc_tich NVARCHAR(50) NULL
    );
END;
GO

IF OBJECT_ID(N'dbo.tai_lieu', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tai_lieu (
        id INT NOT NULL PRIMARY KEY,
        ten_tai_lieu NVARCHAR(255) NULL,
        mo_ta NVARCHAR(255) NULL,
        file_url NVARCHAR(255) NULL,
        gia_ban INT NOT NULL CONSTRAINT DF_tai_lieu_gia_ban DEFAULT 0,
        the_loai_id INT NULL,
        tac_gia_id INT NULL,
        nguoi_dang_id INT NULL,
        CONSTRAINT FK_tai_lieu_the_loai FOREIGN KEY (the_loai_id) REFERENCES dbo.the_loai(id),
        CONSTRAINT FK_tai_lieu_tac_gia FOREIGN KEY (tac_gia_id) REFERENCES dbo.tac_gia(id),
        CONSTRAINT FK_tai_lieu_nguoi_dang FOREIGN KEY (nguoi_dang_id) REFERENCES dbo.nguoi_dung(id)
    );
END;
GO

/* Nang cap cho CSDL da tao truoc day: cot file_url ban dau dung VARCHAR (khong luu duoc
   tieng Viet, bi thay bang '?'). Chuyen sang NVARCHAR de luu dung ten tep co dau. */
IF EXISTS (SELECT 1 FROM sys.columns c
           JOIN sys.types t ON c.user_type_id = t.user_type_id
           WHERE c.object_id = OBJECT_ID(N'dbo.tai_lieu')
             AND c.name = N'file_url'
             AND t.name <> N'nvarchar')
BEGIN
    ALTER TABLE dbo.tai_lieu ALTER COLUMN file_url NVARCHAR(255) NULL;
END;
GO

/* Nang cap duong dan tep trong CSDL: ban cu luu dang ao '/files/ten-tep', nay doi thanh
   duong dan tuong doi tu goc du an 'Content/Document/ten-tep' cho khop voi thu muc that. */
IF OBJECT_ID(N'dbo.tai_lieu', N'U') IS NOT NULL
BEGIN
    UPDATE dbo.tai_lieu
    SET file_url = REPLACE(file_url, 'files/', 'Content/Document/')
    WHERE file_url LIKE '%files/%';
END;
GO

IF OBJECT_ID(N'dbo.giao_dich', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.giao_dich (
        id INT NOT NULL PRIMARY KEY,
        user_id INT NULL,
        tai_lieu_id INT NULL,
        so_tien INT NULL,
        ngay_giao_dich DATETIME NULL,
        trang_thai NVARCHAR(50) NULL,
        CONSTRAINT FK_giao_dich_nguoi_dung FOREIGN KEY (user_id) REFERENCES dbo.nguoi_dung(id),
        CONSTRAINT FK_giao_dich_tai_lieu FOREIGN KEY (tai_lieu_id) REFERENCES dbo.tai_lieu(id)
    );
END;
GO

IF OBJECT_ID(N'dbo.nhat_ky_hoat_dong', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.nhat_ky_hoat_dong (
        id INT NOT NULL PRIMARY KEY,
        user_id INT NULL,
        hanh_dong NVARCHAR(255) NULL,
        thoi_gian DATETIME NULL,
        CONSTRAINT FK_nhat_ky_nguoi_dung FOREIGN KEY (user_id) REFERENCES dbo.nguoi_dung(id)
    );
END;
GO

/* ============================================================
   2. BO SUNG CAC COT QUYEN TRUY CAP CHO TAI_LIEU
   ============================================================ */

IF COL_LENGTH(N'dbo.tai_lieu', N'quyen_doc_online') IS NULL
BEGIN
    ALTER TABLE dbo.tai_lieu
    ADD quyen_doc_online BIT NOT NULL
        CONSTRAINT DF_tai_lieu_quyen_doc DEFAULT 1;
END;
GO

IF COL_LENGTH(N'dbo.tai_lieu', N'quyen_tai') IS NULL
BEGIN
    ALTER TABLE dbo.tai_lieu
    ADD quyen_tai BIT NOT NULL
        CONSTRAINT DF_tai_lieu_quyen_tai DEFAULT 1;
END;
GO

IF COL_LENGTH(N'dbo.tai_lieu', N'trang_thai') IS NULL
BEGIN
    ALTER TABLE dbo.tai_lieu
    ADD trang_thai NVARCHAR(20) NOT NULL
        CONSTRAINT DF_tai_lieu_trang_thai DEFAULT N'HOAT_DONG';
END;
GO

-- Ly do thu thu tu choi tai lieu dong gop (hien thi lai cho nguoi dong gop).
IF COL_LENGTH(N'dbo.tai_lieu', N'ly_do_tu_choi') IS NULL
BEGIN
    ALTER TABLE dbo.tai_lieu
    ADD ly_do_tu_choi NVARCHAR(500) NULL;
END;
GO

/* ============================================================
   3. TAO BANG QUYEN TAI LIEU VA LICH SU XEM/TAI
   ============================================================ */

IF OBJECT_ID(N'dbo.quyen_tai_lieu', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.quyen_tai_lieu (
        id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        user_id INT NOT NULL,
        tai_lieu_id INT NOT NULL,
        giao_dich_id INT NULL,
        ngay_cap DATETIME NOT NULL CONSTRAINT DF_quyen_tai_lieu_ngay_cap DEFAULT GETDATE(),
        trang_thai NVARCHAR(20) NOT NULL CONSTRAINT DF_quyen_tai_lieu_trang_thai DEFAULT N'HOAT_DONG',
        CONSTRAINT FK_quyen_user FOREIGN KEY (user_id) REFERENCES dbo.nguoi_dung(id),
        CONSTRAINT FK_quyen_tai_lieu FOREIGN KEY (tai_lieu_id) REFERENCES dbo.tai_lieu(id),
        CONSTRAINT FK_quyen_giao_dich FOREIGN KEY (giao_dich_id) REFERENCES dbo.giao_dich(id),
        CONSTRAINT UQ_quyen_user_tai_lieu UNIQUE (user_id, tai_lieu_id),
        CONSTRAINT CK_quyen_trang_thai CHECK (trang_thai IN (N'HOAT_DONG', N'DA_THU_HOI'))
    );
END;
GO

IF OBJECT_ID(N'dbo.lich_su_tai_lieu', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.lich_su_tai_lieu (
        id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        user_id INT NOT NULL,
        tai_lieu_id INT NOT NULL,
        hanh_dong NVARCHAR(20) NOT NULL,
        thoi_gian DATETIME NOT NULL CONSTRAINT DF_lich_su_tai_lieu_thoi_gian DEFAULT GETDATE(),
        CONSTRAINT FK_lich_su_user FOREIGN KEY (user_id) REFERENCES dbo.nguoi_dung(id),
        CONSTRAINT FK_lich_su_tai_lieu FOREIGN KEY (tai_lieu_id) REFERENCES dbo.tai_lieu(id),
        CONSTRAINT CK_lich_su_hanh_dong CHECK (hanh_dong IN (N'XEM', N'TAI'))
    );
END;
GO

/* ============================================================
   4. DU LIEU MAU (CHI THEM NEU ID CHUA TON TAI)
   ============================================================ */

IF NOT EXISTS (SELECT 1 FROM dbo.nguoi_dung WHERE id = 1)
    INSERT INTO dbo.nguoi_dung (id, ho_ten, email, mat_khau, so_dien_thoai, vai_tro, trang_thai)
    VALUES (1, N'Nguyễn Văn A', 'nguyenvana@gmail.com', '123456', '0901234567', N'NGUOI_DOC', N'HOAT_DONG');

IF NOT EXISTS (SELECT 1 FROM dbo.nguoi_dung WHERE id = 2)
    INSERT INTO dbo.nguoi_dung (id, ho_ten, email, mat_khau, so_dien_thoai, vai_tro, trang_thai)
    VALUES (2, N'Trần Thị B', 'thuthub@gmail.com', '123456', '0912345678', N'THU_THU', N'HOAT_DONG');

IF NOT EXISTS (SELECT 1 FROM dbo.nguoi_dung WHERE id = 3)
    INSERT INTO dbo.nguoi_dung (id, ho_ten, email, mat_khau, so_dien_thoai, vai_tro, trang_thai)
    VALUES (3, N'Lê Văn C', 'adminc@gmail.com', '123456', '0923456789', N'ADMIN', N'HOAT_DONG');
GO

IF NOT EXISTS (SELECT 1 FROM dbo.the_loai WHERE id = 1)
    INSERT INTO dbo.the_loai (id, ten_the_loai, mo_ta)
    VALUES (1, N'Công nghệ thông tin', N'Tài liệu lập trình, phân tích hệ thống');

IF NOT EXISTS (SELECT 1 FROM dbo.the_loai WHERE id = 2)
    INSERT INTO dbo.the_loai (id, ten_the_loai, mo_ta)
    VALUES (2, N'Kinh tế', N'Sách quản trị, kế toán, tài chính');

IF NOT EXISTS (SELECT 1 FROM dbo.the_loai WHERE id = 3)
    INSERT INTO dbo.the_loai (id, ten_the_loai, mo_ta)
    VALUES (3, N'Văn học', N'Tiểu thuyết, truyện ngắn');
GO

IF NOT EXISTS (SELECT 1 FROM dbo.tac_gia WHERE id = 1)
    INSERT INTO dbo.tac_gia (id, ten_tac_gia, quoc_tich)
    VALUES (1, N'Robert C. Martin', N'Mỹ');

IF NOT EXISTS (SELECT 1 FROM dbo.tac_gia WHERE id = 2)
    INSERT INTO dbo.tac_gia (id, ten_tac_gia, quoc_tich)
    VALUES (2, N'Nguyễn Nhật Ánh', N'Việt Nam');

IF NOT EXISTS (SELECT 1 FROM dbo.tac_gia WHERE id = 3)
    INSERT INTO dbo.tac_gia (id, ten_tac_gia, quoc_tich)
    VALUES (3, N'Ian Sommerville', N'Anh');
GO

IF NOT EXISTS (SELECT 1 FROM dbo.tai_lieu WHERE id = 1)
    INSERT INTO dbo.tai_lieu (id, ten_tai_lieu, mo_ta, file_url, gia_ban, the_loai_id, tac_gia_id, nguoi_dang_id)
    VALUES (1, N'Giáo trình Phân tích Thiết kế Hướng đối tượng', N'Tài liệu OOAD cơ bản', 'Content/Document/ooad.pdf', 0, 1, 3, 2);

IF NOT EXISTS (SELECT 1 FROM dbo.tai_lieu WHERE id = 2)
    INSERT INTO dbo.tai_lieu (id, ten_tai_lieu, mo_ta, file_url, gia_ban, the_loai_id, tac_gia_id, nguoi_dang_id)
    VALUES (2, N'Sách Clean Code', N'Hướng dẫn viết mã sạch', 'Content/Document/cleancode.pdf', 50000, 1, 1, 2);

IF NOT EXISTS (SELECT 1 FROM dbo.tai_lieu WHERE id = 3)
    INSERT INTO dbo.tai_lieu (id, ten_tai_lieu, mo_ta, file_url, gia_ban, the_loai_id, tac_gia_id, nguoi_dang_id)
    VALUES (3, N'Mắt Biếc', N'Tác phẩm văn học nổi tiếng', 'Content/Document/matbiec.pdf', 30000, 3, 2, 2);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.giao_dich WHERE id = 1)
    INSERT INTO dbo.giao_dich (id, user_id, tai_lieu_id, so_tien, ngay_giao_dich, trang_thai)
    VALUES (1, 1, 2, 50000, CONVERT(DATETIME, '2026-10-12T09:30:00', 126), N'THANH_CONG');

IF NOT EXISTS (SELECT 1 FROM dbo.giao_dich WHERE id = 2)
    INSERT INTO dbo.giao_dich (id, user_id, tai_lieu_id, so_tien, ngay_giao_dich, trang_thai)
    VALUES (2, 1, 3, 30000, CONVERT(DATETIME, '2026-10-12T10:15:00', 126), N'THANH_CONG');
GO

IF NOT EXISTS (SELECT 1 FROM dbo.nhat_ky_hoat_dong WHERE id = 1)
    INSERT INTO dbo.nhat_ky_hoat_dong (id, user_id, hanh_dong, thoi_gian)
    VALUES (1, 1, N'Đăng nhập hệ thống', CONVERT(DATETIME, '2026-10-12T09:00:00', 126));

IF NOT EXISTS (SELECT 1 FROM dbo.nhat_ky_hoat_dong WHERE id = 2)
    INSERT INTO dbo.nhat_ky_hoat_dong (id, user_id, hanh_dong, thoi_gian)
    VALUES (2, 2, N'Thêm mới tài liệu Clean Code', CONVERT(DATETIME, '2026-10-12T08:30:00', 126));

IF NOT EXISTS (SELECT 1 FROM dbo.nhat_ky_hoat_dong WHERE id = 3)
    INSERT INTO dbo.nhat_ky_hoat_dong (id, user_id, hanh_dong, thoi_gian)
    VALUES (3, 3, N'Xem danh sách tài khoản', CONVERT(DATETIME, '2026-10-12T11:00:00', 126));
GO

/* ============================================================
   5. RANG BUOC VA INDEX
   ============================================================ */

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_nguoi_dung_email' AND object_id = OBJECT_ID(N'dbo.nguoi_dung'))
BEGIN
    CREATE UNIQUE INDEX UQ_nguoi_dung_email
        ON dbo.nguoi_dung(email)
        WHERE email IS NOT NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_tai_lieu_gia_ban' AND parent_object_id = OBJECT_ID(N'dbo.tai_lieu'))
BEGIN
    ALTER TABLE dbo.tai_lieu
    ADD CONSTRAINT CK_tai_lieu_gia_ban CHECK (gia_ban >= 0);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_giao_dich_so_tien' AND parent_object_id = OBJECT_ID(N'dbo.giao_dich'))
BEGIN
    ALTER TABLE dbo.giao_dich
    ADD CONSTRAINT CK_giao_dich_so_tien CHECK (so_tien >= 0);
END;
GO

/* ============================================================
   6. FUNCTION KIEM TRA QUYEN TAI TAI LIEU
   ============================================================ */

IF OBJECT_ID(N'dbo.fn_CoQuyenTaiTaiLieu', N'FN') IS NOT NULL
    DROP FUNCTION dbo.fn_CoQuyenTaiTaiLieu;
GO

CREATE FUNCTION dbo.fn_CoQuyenTaiTaiLieu
(
    @UserID INT,
    @TaiLieuID INT
)
RETURNS BIT
AS
BEGIN
    DECLARE @CoQuyen BIT = 0;
    DECLARE @GiaBan INT;
    DECLARE @QuyenTai BIT;
    DECLARE @TrangThaiTaiLieu NVARCHAR(20);
    DECLARE @TrangThaiUser NVARCHAR(20);
    DECLARE @VaiTro NVARCHAR(20);

    SELECT
        @GiaBan = gia_ban,
        @QuyenTai = quyen_tai,
        @TrangThaiTaiLieu = trang_thai
    FROM dbo.tai_lieu
    WHERE id = @TaiLieuID;

    SELECT
        @TrangThaiUser = trang_thai,
        @VaiTro = vai_tro
    FROM dbo.nguoi_dung
    WHERE id = @UserID;

    IF @GiaBan IS NULL OR @QuyenTai IS NULL OR @TrangThaiTaiLieu IS NULL
        RETURN 0;

    IF ISNULL(@TrangThaiUser, N'') <> N'HOAT_DONG'
        RETURN 0;

    IF ISNULL(@VaiTro, N'') <> N'NGUOI_DOC'
        RETURN 0;

    IF @TrangThaiTaiLieu <> N'HOAT_DONG' OR @QuyenTai = 0
        RETURN 0;

    IF @GiaBan = 0
        SET @CoQuyen = 1;
    ELSE IF EXISTS (
        SELECT 1
        FROM dbo.quyen_tai_lieu
        WHERE user_id = @UserID
          AND tai_lieu_id = @TaiLieuID
          AND trang_thai = N'HOAT_DONG'
    )
        SET @CoQuyen = 1;

    RETURN @CoQuyen;
END;
GO

/* ============================================================
   7. FUNCTION TINH TONG DOANH THU
   @DenNgay la moc ket thuc khong bao gom.
   ============================================================ */

IF OBJECT_ID(N'dbo.fn_TongDoanhThu', N'FN') IS NOT NULL
    DROP FUNCTION dbo.fn_TongDoanhThu;
GO

CREATE FUNCTION dbo.fn_TongDoanhThu
(
    @TuNgay DATETIME,
    @DenNgay DATETIME
)
RETURNS BIGINT
AS
BEGIN
    DECLARE @Tong BIGINT;

    SELECT @Tong = COALESCE(SUM(CAST(so_tien AS BIGINT)), 0)
    FROM dbo.giao_dich
    WHERE trang_thai = N'THANH_CONG'
      AND ngay_giao_dich >= @TuNgay
      AND ngay_giao_dich < @DenNgay;

    RETURN COALESCE(@Tong, 0);
END;
GO

/* ============================================================
   8. TRIGGER TU CAP QUYEN SAU GIAO DICH THANH CONG
   ============================================================ */

IF OBJECT_ID(N'dbo.trg_GiaoDich_CapQuyenTai', N'TR') IS NOT NULL
    DROP TRIGGER dbo.trg_GiaoDich_CapQuyenTai;
GO

CREATE TRIGGER dbo.trg_GiaoDich_CapQuyenTai
ON dbo.giao_dich
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Neu quyen da ton tai nhung bi thu hoi, giao dich thanh cong moi
    -- co the kich hoat lai quyen.
    UPDATE q
    SET q.trang_thai = N'HOAT_DONG',
        q.giao_dich_id = s.giao_dich_id,
        q.ngay_cap = GETDATE()
    FROM dbo.quyen_tai_lieu AS q
    INNER JOIN (
        SELECT i.user_id, i.tai_lieu_id, MIN(i.id) AS giao_dich_id
        FROM inserted AS i
        INNER JOIN dbo.tai_lieu AS tl ON tl.id = i.tai_lieu_id
        WHERE i.trang_thai = N'THANH_CONG'
          AND tl.gia_ban > 0
        GROUP BY i.user_id, i.tai_lieu_id
    ) AS s
        ON s.user_id = q.user_id
       AND s.tai_lieu_id = q.tai_lieu_id;

    -- Them quyen moi neu nguoi doc chua co ban ghi quyen cho tai lieu.
    INSERT INTO dbo.quyen_tai_lieu (user_id, tai_lieu_id, giao_dich_id)
    SELECT s.user_id, s.tai_lieu_id, s.giao_dich_id
    FROM (
        SELECT i.user_id, i.tai_lieu_id, MIN(i.id) AS giao_dich_id
        FROM inserted AS i
        INNER JOIN dbo.tai_lieu AS tl ON tl.id = i.tai_lieu_id
        WHERE i.trang_thai = N'THANH_CONG'
          AND tl.gia_ban > 0
        GROUP BY i.user_id, i.tai_lieu_id
    ) AS s
    WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.quyen_tai_lieu AS q
        WHERE q.user_id = s.user_id
          AND q.tai_lieu_id = s.tai_lieu_id
    );
END;
GO

/* Cap quyen cho cac giao dich thanh cong da ton tai tu truoc khi co trigger. */
INSERT INTO dbo.quyen_tai_lieu (user_id, tai_lieu_id, giao_dich_id)
SELECT gd.user_id, gd.tai_lieu_id, MIN(gd.id)
FROM dbo.giao_dich AS gd
INNER JOIN dbo.tai_lieu AS tl ON tl.id = gd.tai_lieu_id
WHERE gd.trang_thai = N'THANH_CONG'
  AND tl.gia_ban > 0
  AND NOT EXISTS (
      SELECT 1
      FROM dbo.quyen_tai_lieu AS q
      WHERE q.user_id = gd.user_id
        AND q.tai_lieu_id = gd.tai_lieu_id
  )
GROUP BY gd.user_id, gd.tai_lieu_id;
GO

/* ============================================================
   9. PROCEDURE TAO GIAO DICH
   ============================================================ */

IF OBJECT_ID(N'dbo.sp_TaoGiaoDich', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_TaoGiaoDich;
GO

CREATE PROCEDURE dbo.sp_TaoGiaoDich
    @ID INT,
    @UserID INT,
    @TaiLieuID INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.nguoi_dung
        WHERE id = @UserID
          AND vai_tro = N'NGUOI_DOC'
          AND trang_thai = N'HOAT_DONG'
    )
        THROW 50001, N'Tài khoản không hợp lệ hoặc đã bị khóa.', 1;

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.tai_lieu
        WHERE id = @TaiLieuID
          AND trang_thai = N'HOAT_DONG'
    )
        THROW 50002, N'Tài liệu không tồn tại hoặc không hoạt động.', 1;

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.tai_lieu
        WHERE id = @TaiLieuID
          AND gia_ban > 0
    )
        THROW 50003, N'Tài liệu miễn phí hoặc không có giá bán hợp lệ.', 1;

    IF EXISTS (SELECT 1 FROM dbo.giao_dich WHERE id = @ID)
        THROW 50004, N'Mã giao dịch đã tồn tại.', 1;

    INSERT INTO dbo.giao_dich
        (id, user_id, tai_lieu_id, so_tien, ngay_giao_dich, trang_thai)
    SELECT
        @ID, @UserID, @TaiLieuID, gia_ban, GETDATE(), N'DANG_XU_LY'
    FROM dbo.tai_lieu
    WHERE id = @TaiLieuID;
END;
GO

/* ============================================================
   10. PROCEDURE XAC NHAN GIAO DICH THANH CONG
   Chi goi tu backend sau khi da xac minh thanh toan.
   Trigger se tu dong cap quyen tai lieu.
   ============================================================ */

IF OBJECT_ID(N'dbo.sp_XacNhanGiaoDichThanhCong', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_XacNhanGiaoDichThanhCong;
GO

CREATE PROCEDURE dbo.sp_XacNhanGiaoDichThanhCong
    @GiaoDichID INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE dbo.giao_dich
    SET trang_thai = N'THANH_CONG'
    WHERE id = @GiaoDichID
      AND trang_thai = N'DANG_XU_LY';

    IF @@ROWCOUNT = 0
        THROW 50005, N'Không tìm thấy giao dịch đang xử lý để xác nhận.', 1;
END;
GO

/* ============================================================
   11. PROCEDURE GHI NHAN LUOT XEM / TAI
   ============================================================ */

IF OBJECT_ID(N'dbo.sp_GhiNhanLuotTaiLieu', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GhiNhanLuotTaiLieu;
GO

CREATE PROCEDURE dbo.sp_GhiNhanLuotTaiLieu
    @UserID INT,
    @TaiLieuID INT,
    @HanhDong NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @HanhDong IS NULL OR @HanhDong NOT IN (N'XEM', N'TAI')
        THROW 50010, N'Hành động không hợp lệ.', 1;

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.nguoi_dung
        WHERE id = @UserID
          AND vai_tro = N'NGUOI_DOC'
          AND trang_thai = N'HOAT_DONG'
    )
        THROW 50011, N'Tài khoản không hợp lệ hoặc đã bị khóa.', 1;

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.tai_lieu
        WHERE id = @TaiLieuID
          AND trang_thai = N'HOAT_DONG'
    )
        THROW 50012, N'Tài liệu không tồn tại hoặc không hoạt động.', 1;

    IF @HanhDong = N'TAI'
       AND dbo.fn_CoQuyenTaiTaiLieu(@UserID, @TaiLieuID) = 0
        THROW 50013, N'Bạn không có quyền tải tài liệu này.', 1;

    IF @HanhDong = N'XEM'
    BEGIN
        IF NOT EXISTS (
            SELECT 1
            FROM dbo.tai_lieu
            WHERE id = @TaiLieuID
              AND quyen_doc_online = 1
        )
            THROW 50014, N'Tài liệu không cho phép đọc trực tuyến.', 1;

        IF EXISTS (
            SELECT 1
            FROM dbo.tai_lieu
            WHERE id = @TaiLieuID
              AND gia_ban > 0
        )
        AND NOT EXISTS (
            SELECT 1
            FROM dbo.quyen_tai_lieu
            WHERE user_id = @UserID
              AND tai_lieu_id = @TaiLieuID
              AND trang_thai = N'HOAT_DONG'
        )
            THROW 50015, N'Bạn chưa có quyền truy cập tài liệu trả phí.', 1;
    END;

    BEGIN TRY
        BEGIN TRANSACTION;

        INSERT INTO dbo.lich_su_tai_lieu (user_id, tai_lieu_id, hanh_dong)
        VALUES (@UserID, @TaiLieuID, @HanhDong);

        -- Bang nhat_ky_hoat_dong giu ID thu cong nhu schema ban dau.
        -- Khoa bang khi lay MAX(id)+1 de tranh trung ID khi dong thoi.
        DECLARE @LogID INT;
        SELECT @LogID = ISNULL(MAX(id), 0) + 1
        FROM dbo.nhat_ky_hoat_dong WITH (TABLOCKX, HOLDLOCK);

        INSERT INTO dbo.nhat_ky_hoat_dong (id, user_id, hanh_dong, thoi_gian)
        VALUES (
            @LogID,
            @UserID,
            CASE
                WHEN @HanhDong = N'XEM'
                    THEN N'Xem tài liệu ID ' + CAST(@TaiLieuID AS NVARCHAR(20))
                ELSE N'Tải tài liệu ID ' + CAST(@TaiLieuID AS NVARCHAR(20))
            END,
            GETDATE()
        );

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

/* ============================================================
   12. PROCEDURE THONG KE DOANH THU
   ============================================================ */

IF OBJECT_ID(N'dbo.sp_ThongKeDoanhThu', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_ThongKeDoanhThu;
GO

CREATE PROCEDURE dbo.sp_ThongKeDoanhThu
    @TuNgay DATETIME,
    @DenNgay DATETIME
AS
BEGIN
    SET NOCOUNT ON;

    IF @TuNgay IS NULL OR @DenNgay IS NULL OR @TuNgay >= @DenNgay
        THROW 50020, N'Khoảng thời gian thống kê không hợp lệ.', 1;

    SELECT
        COUNT(*) AS SoGiaoDichThanhCong,
        COALESCE(SUM(CAST(so_tien AS BIGINT)), 0) AS TongDoanhThu
    FROM dbo.giao_dich
    WHERE trang_thai = N'THANH_CONG'
      AND ngay_giao_dich >= @TuNgay
      AND ngay_giao_dich < @DenNgay;
END;
GO

/* ============================================================
   13. PROCEDURE LOC NHAT KY HOAT DONG
   ============================================================ */

IF OBJECT_ID(N'dbo.sp_LocNhatKy', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_LocNhatKy;
GO

CREATE PROCEDURE dbo.sp_LocNhatKy
    @UserID INT = NULL,
    @TuNgay DATETIME = NULL,
    @DenNgay DATETIME = NULL,
    @TuKhoa NVARCHAR(255) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @TuNgay IS NOT NULL AND @DenNgay IS NOT NULL AND @TuNgay >= @DenNgay
        THROW 50021, N'Khoảng thời gian lọc nhật ký không hợp lệ.', 1;

    SELECT id, user_id, hanh_dong, thoi_gian
    FROM dbo.nhat_ky_hoat_dong
    WHERE (@UserID IS NULL OR user_id = @UserID)
      AND (@TuNgay IS NULL OR thoi_gian >= @TuNgay)
      AND (@DenNgay IS NULL OR thoi_gian < @DenNgay)
      AND (@TuKhoa IS NULL OR hanh_dong LIKE N'%' + @TuKhoa + N'%')
    ORDER BY thoi_gian DESC;
END;
GO

/* ============================================================
   14. CAC CAU LENH KIEM TRA
   ============================================================ */

-- Xem cac giao dich
SELECT * FROM dbo.giao_dich ORDER BY id;
GO

-- Kiem tra quyen tai cua nguoi doc 1 voi tai lieu mien phi ID 1
SELECT dbo.fn_CoQuyenTaiTaiLieu(1, 1) AS CoQuyenTaiTaiLieuMienPhi;
GO

-- Kiem tra quyen tai voi tai lieu ID 2 da co giao dich thanh cong mau
SELECT dbo.fn_CoQuyenTaiTaiLieu(1, 2) AS CoQuyenTaiTaiLieuTraPhi;
GO

-- Thong ke doanh thu ngay 12/10/2026; du lieu mau du kien la 80000
DECLARE @NgayBatDauThongKe DATETIME = CONVERT(DATETIME, '2026-10-12T00:00:00', 126);
DECLARE @NgayKetThucThongKe DATETIME = CONVERT(DATETIME, '2026-10-13T00:00:00', 126);

EXEC dbo.sp_ThongKeDoanhThu
    @TuNgay = @NgayBatDauThongKe,
    @DenNgay = @NgayKetThucThongKe;
GO

-- Tinh doanh thu bang function
SELECT dbo.fn_TongDoanhThu(CONVERT(DATETIME, '2026-10-12T00:00:00', 126), CONVERT(DATETIME, '2026-10-13T00:00:00', 126)) AS TongDoanhThu;
GO

-- Xem nhat ky cua nguoi dung 1 trong ngay 12/10/2026
DECLARE @NgayBatDauLoc DATETIME = CONVERT(DATETIME, '2026-10-12T00:00:00', 126);
DECLARE @NgayKetThucLoc DATETIME = CONVERT(DATETIME, '2026-10-13T00:00:00', 126);

EXEC dbo.sp_LocNhatKy
    @UserID = 1,
    @TuNgay = @NgayBatDauLoc,
    @DenNgay = @NgayKetThucLoc,
    @TuKhoa = NULL;
GO

USE QuanLyThuVien;
GO

/* ============================================================
   15. BO SUNG DU LIEU MAU CHO CAC CHUC NANG NGUOI DOC
   - Co the chay lai nhieu lan: moi dong deu duoc kiem tra ton tai.
   - Du lieu duoc thiet ke de kiem thu day du cac chuc nang:
     dang ky / dang nhap, tim kiem - loc, goi y theo mo ta,
     xem chi tiet va gia, doc truc tuyen, tai ve, mua quyen tai,
     lich su giao dich va quyen tai, cap nhat ho so - doi mat khau.
   ============================================================ */

/* 15.1. THE LOAI BO SUNG */
IF NOT EXISTS (SELECT 1 FROM dbo.the_loai WHERE id = 4)
    INSERT INTO dbo.the_loai (id, ten_the_loai, mo_ta)
    VALUES (4, N'Kỹ năng sống', N'Giao tiếp, quản lý thời gian, phát triển bản thân');

IF NOT EXISTS (SELECT 1 FROM dbo.the_loai WHERE id = 5)
    INSERT INTO dbo.the_loai (id, ten_the_loai, mo_ta)
    VALUES (5, N'Ngoại ngữ', N'Tiếng Anh, luyện thi chứng chỉ, giao tiếp');

IF NOT EXISTS (SELECT 1 FROM dbo.the_loai WHERE id = 6)
    INSERT INTO dbo.the_loai (id, ten_the_loai, mo_ta)
    VALUES (6, N'Khoa học - Kỹ thuật', N'Trí tuệ nhân tạo, dữ liệu, vật lý, kỹ thuật');

IF NOT EXISTS (SELECT 1 FROM dbo.the_loai WHERE id = 7)
    INSERT INTO dbo.the_loai (id, ten_the_loai, mo_ta)
    VALUES (7, N'Thiếu nhi', N'Truyện thiếu nhi, sách giáo dục trẻ em');

IF NOT EXISTS (SELECT 1 FROM dbo.the_loai WHERE id = 8)
    INSERT INTO dbo.the_loai (id, ten_the_loai, mo_ta)
    VALUES (8, N'Lịch sử - Địa lý', N'Lịch sử thế giới, nhân loại học, địa lý');
GO

/* 15.2. TAC GIA BO SUNG */
IF NOT EXISTS (SELECT 1 FROM dbo.tac_gia WHERE id = 4)
    INSERT INTO dbo.tac_gia (id, ten_tac_gia, quoc_tich) VALUES (4, N'Joshua Bloch', N'Mỹ');
IF NOT EXISTS (SELECT 1 FROM dbo.tac_gia WHERE id = 5)
    INSERT INTO dbo.tac_gia (id, ten_tac_gia, quoc_tich) VALUES (5, N'Andrew Hunt', N'Mỹ');
IF NOT EXISTS (SELECT 1 FROM dbo.tac_gia WHERE id = 6)
    INSERT INTO dbo.tac_gia (id, ten_tac_gia, quoc_tich) VALUES (6, N'Nguyễn Ngọc Tư', N'Việt Nam');
IF NOT EXISTS (SELECT 1 FROM dbo.tac_gia WHERE id = 7)
    INSERT INTO dbo.tac_gia (id, ten_tac_gia, quoc_tich) VALUES (7, N'Haruki Murakami', N'Nhật Bản');
IF NOT EXISTS (SELECT 1 FROM dbo.tac_gia WHERE id = 8)
    INSERT INTO dbo.tac_gia (id, ten_tac_gia, quoc_tich) VALUES (8, N'Dale Carnegie', N'Mỹ');
IF NOT EXISTS (SELECT 1 FROM dbo.tac_gia WHERE id = 9)
    INSERT INTO dbo.tac_gia (id, ten_tac_gia, quoc_tich) VALUES (9, N'Nguyễn Hiến Lê', N'Việt Nam');
IF NOT EXISTS (SELECT 1 FROM dbo.tac_gia WHERE id = 10)
    INSERT INTO dbo.tac_gia (id, ten_tac_gia, quoc_tich) VALUES (10, N'Daniel Kahneman', N'Mỹ');
IF NOT EXISTS (SELECT 1 FROM dbo.tac_gia WHERE id = 11)
    INSERT INTO dbo.tac_gia (id, ten_tac_gia, quoc_tich) VALUES (11, N'Tô Hoài', N'Việt Nam');
IF NOT EXISTS (SELECT 1 FROM dbo.tac_gia WHERE id = 12)
    INSERT INTO dbo.tac_gia (id, ten_tac_gia, quoc_tich) VALUES (12, N'Yuval Noah Harari', N'Israel');
IF NOT EXISTS (SELECT 1 FROM dbo.tac_gia WHERE id = 13)
    INSERT INTO dbo.tac_gia (id, ten_tac_gia, quoc_tich) VALUES (13, N'Ernest Hemingway', N'Mỹ');
IF NOT EXISTS (SELECT 1 FROM dbo.tac_gia WHERE id = 14)
    INSERT INTO dbo.tac_gia (id, ten_tac_gia, quoc_tich) VALUES (14, N'Antoine de Saint-Exupéry', N'Pháp');
IF NOT EXISTS (SELECT 1 FROM dbo.tac_gia WHERE id = 15)
    INSERT INTO dbo.tac_gia (id, ten_tac_gia, quoc_tich) VALUES (15, N'Stephen Covey', N'Mỹ');
IF NOT EXISTS (SELECT 1 FROM dbo.tac_gia WHERE id = 16)
    INSERT INTO dbo.tac_gia (id, ten_tac_gia, quoc_tich) VALUES (16, N'Adam Khoo', N'Singapore');
IF NOT EXISTS (SELECT 1 FROM dbo.tac_gia WHERE id = 17)
    INSERT INTO dbo.tac_gia (id, ten_tac_gia, quoc_tich) VALUES (17, N'Raymond Murphy', N'Anh');
GO

/* 15.3. NGUOI DUNG BO SUNG (mat khau mau dang plaintext se tu nang cap PBKDF2 khi dang nhap) */
IF NOT EXISTS (SELECT 1 FROM dbo.nguoi_dung WHERE id = 4)
    INSERT INTO dbo.nguoi_dung (id, ho_ten, email, mat_khau, so_dien_thoai, vai_tro, trang_thai)
    VALUES (4, N'Phạm Thị D', 'phamthid@gmail.com', '123456', '0934567890', N'NGUOI_DOC', N'HOAT_DONG');

IF NOT EXISTS (SELECT 1 FROM dbo.nguoi_dung WHERE id = 5)
    INSERT INTO dbo.nguoi_dung (id, ho_ten, email, mat_khau, so_dien_thoai, vai_tro, trang_thai)
    VALUES (5, N'Hoàng Văn E', 'hoangvane@gmail.com', '123456', '0945678901', N'NGUOI_DOC', N'HOAT_DONG');

IF NOT EXISTS (SELECT 1 FROM dbo.nguoi_dung WHERE id = 6)
    INSERT INTO dbo.nguoi_dung (id, ho_ten, email, mat_khau, so_dien_thoai, vai_tro, trang_thai)
    VALUES (6, N'Vũ Thị F', 'vuthif@gmail.com', '123456', '0956789012', N'NGUOI_DOC', N'HOAT_DONG');

-- Tai khoan bi khoa: dung de kiem thu man hinh dang nhap tu choi.
IF NOT EXISTS (SELECT 1 FROM dbo.nguoi_dung WHERE id = 7)
    INSERT INTO dbo.nguoi_dung (id, ho_ten, email, mat_khau, so_dien_thoai, vai_tro, trang_thai)
    VALUES (7, N'Đỗ Minh G', 'dominhanh@gmail.com', '123456', '0967890123', N'NGUOI_DOC', N'BI_KHOA');

IF NOT EXISTS (SELECT 1 FROM dbo.nguoi_dung WHERE id = 8)
    INSERT INTO dbo.nguoi_dung (id, ho_ten, email, mat_khau, so_dien_thoai, vai_tro, trang_thai)
    VALUES (8, N'Bùi Thị H', 'buithih@gmail.com', '123456', '0978901234', N'NGUOI_DOC', N'HOAT_DONG');
GO

/* 15.4. TAI LIEU BO SUNG
   - quyen_doc_online / quyen_tai / trang_thai duoc ghi ro de kiem thu phan quyen.
   - file_url la duong dan tuong doi tu goc du an (Content/Document/ten-tep); tep that nam
     trong thu muc Content/Document (bi chan truy cap truc tiep tu IIS, moi truy cap phai
     qua controller de kiem tra quyen). */
IF NOT EXISTS (SELECT 1 FROM dbo.tai_lieu WHERE id = 4)
    INSERT INTO dbo.tai_lieu (id, ten_tai_lieu, mo_ta, file_url, gia_ban, the_loai_id, tac_gia_id, nguoi_dang_id, quyen_doc_online, quyen_tai, trang_thai)
    VALUES (4, N'Lập trình hướng đối tượng với Java',
            N'Giáo trình lập trình hướng đối tượng Java: lớp, đối tượng, kế thừa, đa hình, interface, collection, đa luồng và bài tập thực hành cho sinh viên công nghệ thông tin.',
            'Content/Document/lap-trinh-huong-doi-tuong-java.txt', 45000, 1, 4, 2, 1, 1, N'HOAT_DONG');

IF NOT EXISTS (SELECT 1 FROM dbo.tai_lieu WHERE id = 5)
    INSERT INTO dbo.tai_lieu (id, ten_tai_lieu, mo_ta, file_url, gia_ban, the_loai_id, tac_gia_id, nguoi_dang_id, quyen_doc_online, quyen_tai, trang_thai)
    VALUES (5, N'Cấu trúc dữ liệu và giải thuật',
            N'Tài liệu cấu trúc dữ liệu: mảng, danh sách liên kết, ngăn xếp, hàng đợi, cây nhị phân, đồ thị, thuật toán sắp xếp, tìm kiếm và phân tích độ phức tạp.',
            'Content/Document/cau-truc-du-lieu-va-giai-thuat.txt', 60000, 1, 5, 2, 1, 1, N'HOAT_DONG');

IF NOT EXISTS (SELECT 1 FROM dbo.tai_lieu WHERE id = 6)
    INSERT INTO dbo.tai_lieu (id, ten_tai_lieu, mo_ta, file_url, gia_ban, the_loai_id, tac_gia_id, nguoi_dang_id, quyen_doc_online, quyen_tai, trang_thai)
    VALUES (6, N'Lập trình C# căn bản',
            N'Sách học lập trình C# từ đầu: biến, kiểu dữ liệu, câu lệnh điều kiện, vòng lặp, hàm, lớp, kế thừa, LINQ, lập trình bất đồng bộ async await và bài tập.',
            'Content/Document/lap-trinh-c-sharp-can-ban.txt', 0, 1, 5, 2, 1, 1, N'HOAT_DONG');

IF NOT EXISTS (SELECT 1 FROM dbo.tai_lieu WHERE id = 7)
    INSERT INTO dbo.tai_lieu (id, ten_tai_lieu, mo_ta, file_url, gia_ban, the_loai_id, tac_gia_id, nguoi_dang_id, quyen_doc_online, quyen_tai, trang_thai)
    VALUES (7, N'Cơ sở dữ liệu quan hệ và SQL',
            N'Giáo trình cơ sở dữ liệu: mô hình quan hệ, khóa, chuẩn hóa, chỉ mục, truy vấn SQL, hàm, thủ tục lưu trữ, trigger và tối ưu hiệu năng truy vấn.',
            'Content/Document/co-so-du-lieu-quan-he-va-sql.txt', 55000, 1, 3, 2, 1, 1, N'HOAT_DONG');

-- Tai lieu CHI cho phep tai ve, khong cho doc truc tuyen (quyen_doc_online = 0).
IF NOT EXISTS (SELECT 1 FROM dbo.tai_lieu WHERE id = 8)
    INSERT INTO dbo.tai_lieu (id, ten_tai_lieu, mo_ta, file_url, gia_ban, the_loai_id, tac_gia_id, nguoi_dang_id, quyen_doc_online, quyen_tai, trang_thai)
    VALUES (8, N'Nguyên lý thiết kế phần mềm',
            N'Phân tích thiết kế phần mềm: khảo sát yêu cầu, kiến trúc hệ thống, mẫu thiết kế, sơ đồ UML, kiểm thử và quy trình phát triển phần mềm.',
            'Content/Document/nguyen-ly-thiet-ke-phan-mem.txt', 50000, 1, 3, 2, 0, 1, N'HOAT_DONG');

IF NOT EXISTS (SELECT 1 FROM dbo.tai_lieu WHERE id = 9)
    INSERT INTO dbo.tai_lieu (id, ten_tai_lieu, mo_ta, file_url, gia_ban, the_loai_id, tac_gia_id, nguoi_dang_id, quyen_doc_online, quyen_tai, trang_thai)
    VALUES (9, N'Clean Architecture thực hành',
            N'Sách kiến trúc phần mềm: nguyên lý SOLID, ranh giới kiến trúc, dependency inversion, phân lớp ứng dụng và ví dụ tái cấu trúc mã nguồn.',
            'Content/Document/clean-architecture-thuc-hanh.txt', 68000, 1, 1, 2, 1, 1, N'HOAT_DONG');

IF NOT EXISTS (SELECT 1 FROM dbo.tai_lieu WHERE id = 10)
    INSERT INTO dbo.tai_lieu (id, ten_tai_lieu, mo_ta, file_url, gia_ban, the_loai_id, tac_gia_id, nguoi_dang_id, quyen_doc_online, quyen_tai, trang_thai)
    VALUES (10, N'Trí tuệ nhân tạo nhập môn',
            N'Tài liệu nhập môn trí tuệ nhân tạo và học máy: tác tử thông minh, thuật toán tìm kiếm, biểu diễn tri thức, mạng nơ-ron, học sâu, xử lý ngôn ngữ tự nhiên.',
            'Content/Document/tri-tue-nhan-tao-nhap-mon.txt', 75000, 6, 3, 2, 1, 1, N'HOAT_DONG');

IF NOT EXISTS (SELECT 1 FROM dbo.tai_lieu WHERE id = 11)
    INSERT INTO dbo.tai_lieu (id, ten_tai_lieu, mo_ta, file_url, gia_ban, the_loai_id, tac_gia_id, nguoi_dang_id, quyen_doc_online, quyen_tai, trang_thai)
    VALUES (11, N'Tư duy nhanh và chậm',
            N'Sách kinh tế học hành vi về tư duy nhanh và chậm, thiên kiến nhận thức, tâm lý ra quyết định, ứng dụng trong quản trị, đầu tư và tài chính.',
            'Content/Document/tu-duy-nhanh-va-cham.txt', 89000, 2, 10, 2, 1, 1, N'HOAT_DONG');

IF NOT EXISTS (SELECT 1 FROM dbo.tai_lieu WHERE id = 12)
    INSERT INTO dbo.tai_lieu (id, ten_tai_lieu, mo_ta, file_url, gia_ban, the_loai_id, tac_gia_id, nguoi_dang_id, quyen_doc_online, quyen_tai, trang_thai)
    VALUES (12, N'Đắc nhân tâm',
            N'Sách kỹ năng sống kinh điển về giao tiếp, ứng xử, thu phục lòng người, nghệ thuật khen ngợi và xây dựng quan hệ tốt trong công việc.',
            'Content/Document/dac-nhan-tam.txt', 0, 4, 8, 2, 1, 1, N'HOAT_DONG');

IF NOT EXISTS (SELECT 1 FROM dbo.tai_lieu WHERE id = 13)
    INSERT INTO dbo.tai_lieu (id, ten_tai_lieu, mo_ta, file_url, gia_ban, the_loai_id, tac_gia_id, nguoi_dang_id, quyen_doc_online, quyen_tai, trang_thai)
    VALUES (13, N'Bảy thói quen để thành đạt',
            N'Sách phát triển bản thân về bảy thói quen hiệu quả, quản lý thời gian, mục tiêu cá nhân, ưu tiên công việc và lãnh đạo bản thân.',
            'Content/Document/bay-thoi-quen-de-thanh-dat.txt', 65000, 4, 15, 2, 1, 1, N'HOAT_DONG');

IF NOT EXISTS (SELECT 1 FROM dbo.tai_lieu WHERE id = 14)
    INSERT INTO dbo.tai_lieu (id, ten_tai_lieu, mo_ta, file_url, gia_ban, the_loai_id, tac_gia_id, nguoi_dang_id, quyen_doc_online, quyen_tai, trang_thai)
    VALUES (14, N'Tôi tài giỏi, bạn cũng thế',
            N'Sách kỹ năng học tập và phát triển bản thân: phương pháp học hiệu quả, sơ đồ tư duy, quản lý thời gian, đặt mục tiêu và tạo động lực học tập.',
            'Content/Document/toi-tai-gioi-ban-cung-the.txt', 0, 4, 16, 2, 1, 1, N'HOAT_DONG');

-- Tai lieu CHI doc truc tuyen, khong cho tai ve (quyen_tai = 0).
IF NOT EXISTS (SELECT 1 FROM dbo.tai_lieu WHERE id = 15)
    INSERT INTO dbo.tai_lieu (id, ten_tai_lieu, mo_ta, file_url, gia_ban, the_loai_id, tac_gia_id, nguoi_dang_id, quyen_doc_online, quyen_tai, trang_thai)
    VALUES (15, N'Kỹ năng thuyết trình hiệu quả',
            N'Tài liệu kỹ năng mềm về thuyết trình: chuẩn bị nội dung, thiết kế slide, ngôn ngữ cơ thể, kiểm soát sự hồi hộp và trả lời câu hỏi.',
            'Content/Document/ky-nang-thuyet-trinh-hieu-qua.txt', 0, 4, 8, 2, 1, 0, N'HOAT_DONG');

IF NOT EXISTS (SELECT 1 FROM dbo.tai_lieu WHERE id = 16)
    INSERT INTO dbo.tai_lieu (id, ten_tai_lieu, mo_ta, file_url, gia_ban, the_loai_id, tac_gia_id, nguoi_dang_id, quyen_doc_online, quyen_tai, trang_thai)
    VALUES (16, N'Tôi thấy hoa vàng trên cỏ xanh',
            N'Truyện dài văn học thiếu nhi Việt Nam về tuổi thơ, tình anh em, tình bạn, mái trường và những ký ức miền quê.',
            'Content/Document/toi-thay-hoa-vang-tren-co-xanh.txt', 42000, 3, 2, 2, 1, 1, N'HOAT_DONG');

IF NOT EXISTS (SELECT 1 FROM dbo.tai_lieu WHERE id = 17)
    INSERT INTO dbo.tai_lieu (id, ten_tai_lieu, mo_ta, file_url, gia_ban, the_loai_id, tac_gia_id, nguoi_dang_id, quyen_doc_online, quyen_tai, trang_thai)
    VALUES (17, N'Cánh đồng bất tận',
            N'Tập truyện ngắn văn học Việt Nam về miền Tây sông nước, thân phận con người, tình người và nỗi đau trong cuộc sống mưu sinh.',
            'Content/Document/canh-dong-bat-tan.txt', 38000, 3, 6, 2, 1, 1, N'HOAT_DONG');

IF NOT EXISTS (SELECT 1 FROM dbo.tai_lieu WHERE id = 18)
    INSERT INTO dbo.tai_lieu (id, ten_tai_lieu, mo_ta, file_url, gia_ban, the_loai_id, tac_gia_id, nguoi_dang_id, quyen_doc_online, quyen_tai, trang_thai)
    VALUES (18, N'Rừng Na Uy',
            N'Tiểu thuyết văn học Nhật Bản về tuổi trẻ, tình yêu, cô đơn, mất mát và hành trình trưởng thành của nhân vật.',
            'Content/Document/rung-na-uy.txt', 72000, 3, 7, 2, 1, 1, N'HOAT_DONG');

IF NOT EXISTS (SELECT 1 FROM dbo.tai_lieu WHERE id = 19)
    INSERT INTO dbo.tai_lieu (id, ten_tai_lieu, mo_ta, file_url, gia_ban, the_loai_id, tac_gia_id, nguoi_dang_id, quyen_doc_online, quyen_tai, trang_thai)
    VALUES (19, N'Hoàng tử bé',
            N'Truyện văn học kinh điển thế giới về tình bạn, ý nghĩa cuộc sống, sự ngây thơ và góc nhìn trong trẻo của trẻ thơ.',
            'Content/Document/hoang-tu-be.txt', 0, 3, 14, 2, 1, 1, N'HOAT_DONG');

IF NOT EXISTS (SELECT 1 FROM dbo.tai_lieu WHERE id = 20)
    INSERT INTO dbo.tai_lieu (id, ten_tai_lieu, mo_ta, file_url, gia_ban, the_loai_id, tac_gia_id, nguoi_dang_id, quyen_doc_online, quyen_tai, trang_thai)
    VALUES (20, N'Dế Mèn phiêu lưu ký',
            N'Truyện thiếu nhi kinh điển Việt Nam về chú Dế Mèn, hành trình phiêu lưu, tình bạn, lòng dũng cảm và bài học làm người.',
            'Content/Document/de-men-phieu-luu-ky.txt', 0, 7, 11, 2, 1, 1, N'HOAT_DONG');

IF NOT EXISTS (SELECT 1 FROM dbo.tai_lieu WHERE id = 21)
    INSERT INTO dbo.tai_lieu (id, ten_tai_lieu, mo_ta, file_url, gia_ban, the_loai_id, tac_gia_id, nguoi_dang_id, quyen_doc_online, quyen_tai, trang_thai)
    VALUES (21, N'Sapiens: Lược sử loài người',
            N'Sách lịch sử và nhân loại học về lược sử loài người, cách mạng nhận thức, nông nghiệp, khoa học và dự báo tương lai nhân loại.',
            'Content/Document/sapiens-luoc-su-loai-nguoi.txt', 95000, 8, 12, 2, 1, 1, N'HOAT_DONG');

IF NOT EXISTS (SELECT 1 FROM dbo.tai_lieu WHERE id = 22)
    INSERT INTO dbo.tai_lieu (id, ten_tai_lieu, mo_ta, file_url, gia_ban, the_loai_id, tac_gia_id, nguoi_dang_id, quyen_doc_online, quyen_tai, trang_thai)
    VALUES (22, N'Tiếng Anh giao tiếp cơ bản',
            N'Giáo trình ngoại ngữ tiếng Anh giao tiếp: ngữ pháp cơ bản, từ vựng theo chủ đề, mẫu câu thông dụng, luyện nghe nói và bài tập thực hành.',
            'Content/Document/tieng-anh-giao-tiep-co-ban.txt', 0, 5, 17, 2, 1, 1, N'HOAT_DONG');

IF NOT EXISTS (SELECT 1 FROM dbo.tai_lieu WHERE id = 23)
    INSERT INTO dbo.tai_lieu (id, ten_tai_lieu, mo_ta, file_url, gia_ban, the_loai_id, tac_gia_id, nguoi_dang_id, quyen_doc_online, quyen_tai, trang_thai)
    VALUES (23, N'Luyện thi TOEIC 600+',
            N'Sách luyện thi tiếng Anh TOEIC: từ vựng, ngữ pháp, kỹ năng nghe đọc, đề thi thử, chiến lược làm bài và mẹo đạt 600 điểm.',
            'Content/Document/luyen-thi-toeic-600.txt', 88000, 5, 17, 2, 1, 1, N'HOAT_DONG');

-- Tai lieu NGUNG_HOAT_DONG: khong duoc hien thi cho nguoi doc, dung de kiem thu an du lieu.
IF NOT EXISTS (SELECT 1 FROM dbo.tai_lieu WHERE id = 24)
    INSERT INTO dbo.tai_lieu (id, ten_tai_lieu, mo_ta, file_url, gia_ban, the_loai_id, tac_gia_id, nguoi_dang_id, quyen_doc_online, quyen_tai, trang_thai)
    VALUES (24, N'Bản thảo nội bộ chưa phát hành',
            N'Bản thảo nội bộ đang chờ thủ thư kiểm duyệt, không công bố cho người đọc.',
            'Content/Document/ban-thao-noi-bo.txt', 0, 1, 1, 2, 1, 1, N'NGUNG_HOAT_DONG');
GO

/* 15.5. GIAO DICH BO SUNG
   Cac giao dich THANH_CONG se kich hoat trigger cap quyen tai tu dong. */
IF NOT EXISTS (SELECT 1 FROM dbo.giao_dich WHERE id = 3)
    INSERT INTO dbo.giao_dich (id, user_id, tai_lieu_id, so_tien, ngay_giao_dich, trang_thai)
    VALUES (3, 4, 4, 45000, CONVERT(DATETIME, '2026-10-15T08:20:00', 126), N'THANH_CONG');

IF NOT EXISTS (SELECT 1 FROM dbo.giao_dich WHERE id = 4)
    INSERT INTO dbo.giao_dich (id, user_id, tai_lieu_id, so_tien, ngay_giao_dich, trang_thai)
    VALUES (4, 5, 10, 75000, CONVERT(DATETIME, '2026-10-16T09:05:00', 126), N'THANH_CONG');

IF NOT EXISTS (SELECT 1 FROM dbo.giao_dich WHERE id = 5)
    INSERT INTO dbo.giao_dich (id, user_id, tai_lieu_id, so_tien, ngay_giao_dich, trang_thai)
    VALUES (5, 4, 7, 55000, CONVERT(DATETIME, '2026-10-17T14:30:00', 126), N'THANH_CONG');

IF NOT EXISTS (SELECT 1 FROM dbo.giao_dich WHERE id = 6)
    INSERT INTO dbo.giao_dich (id, user_id, tai_lieu_id, so_tien, ngay_giao_dich, trang_thai)
    VALUES (6, 6, 11, 89000, CONVERT(DATETIME, '2026-10-18T10:45:00', 126), N'THANH_CONG');

IF NOT EXISTS (SELECT 1 FROM dbo.giao_dich WHERE id = 7)
    INSERT INTO dbo.giao_dich (id, user_id, tai_lieu_id, so_tien, ngay_giao_dich, trang_thai)
    VALUES (7, 5, 21, 95000, CONVERT(DATETIME, '2026-10-19T16:10:00', 126), N'THANH_CONG');

IF NOT EXISTS (SELECT 1 FROM dbo.giao_dich WHERE id = 8)
    INSERT INTO dbo.giao_dich (id, user_id, tai_lieu_id, so_tien, ngay_giao_dich, trang_thai)
    VALUES (8, 8, 23, 88000, CONVERT(DATETIME, '2026-10-20T09:40:00', 126), N'THANH_CONG');

-- Giao dich dang cho thanh toan cua nguoi doc 1: dung de kiem thu man hinh mua quyen tai.
IF NOT EXISTS (SELECT 1 FROM dbo.giao_dich WHERE id = 9)
    INSERT INTO dbo.giao_dich (id, user_id, tai_lieu_id, so_tien, ngay_giao_dich, trang_thai)
    VALUES (9, 1, 5, 60000, CONVERT(DATETIME, '2026-10-21T11:25:00', 126), N'DANG_XU_LY');

-- Giao dich that bai: dung de kiem thu hien thi trang thai.
IF NOT EXISTS (SELECT 1 FROM dbo.giao_dich WHERE id = 10)
    INSERT INTO dbo.giao_dich (id, user_id, tai_lieu_id, so_tien, ngay_giao_dich, trang_thai)
    VALUES (10, 6, 18, 72000, CONVERT(DATETIME, '2026-10-22T15:00:00', 126), N'THAT_BAI');

IF NOT EXISTS (SELECT 1 FROM dbo.giao_dich WHERE id = 11)
    INSERT INTO dbo.giao_dich (id, user_id, tai_lieu_id, so_tien, ngay_giao_dich, trang_thai)
    VALUES (11, 8, 9, 68000, CONVERT(DATETIME, '2026-10-22T16:35:00', 126), N'THANH_CONG');
GO

/* 15.6. QUYEN TAI DA BI THU HOI (minh hoa trang thai DA_THU_HOI) */
IF NOT EXISTS (SELECT 1 FROM dbo.quyen_tai_lieu WHERE user_id = 6 AND tai_lieu_id = 4)
    INSERT INTO dbo.quyen_tai_lieu (user_id, tai_lieu_id, giao_dich_id, ngay_cap, trang_thai)
    VALUES (6, 4, NULL, CONVERT(DATETIME, '2026-09-01T10:00:00', 126), N'DA_THU_HOI');
GO

/* 15.7. LICH SU DOC / TAI BO SUNG */
IF NOT EXISTS (SELECT 1 FROM dbo.lich_su_tai_lieu WHERE user_id = 1 AND tai_lieu_id = 1 AND hanh_dong = N'XEM')
    INSERT INTO dbo.lich_su_tai_lieu (user_id, tai_lieu_id, hanh_dong, thoi_gian)
    VALUES (1, 1, N'XEM', CONVERT(DATETIME, '2026-10-14T20:15:00', 126));

IF NOT EXISTS (SELECT 1 FROM dbo.lich_su_tai_lieu WHERE user_id = 1 AND tai_lieu_id = 6 AND hanh_dong = N'XEM')
    INSERT INTO dbo.lich_su_tai_lieu (user_id, tai_lieu_id, hanh_dong, thoi_gian)
    VALUES (1, 6, N'XEM', CONVERT(DATETIME, '2026-10-21T21:00:00', 126));

IF NOT EXISTS (SELECT 1 FROM dbo.lich_su_tai_lieu WHERE user_id = 1 AND tai_lieu_id = 2 AND hanh_dong = N'TAI')
    INSERT INTO dbo.lich_su_tai_lieu (user_id, tai_lieu_id, hanh_dong, thoi_gian)
    VALUES (1, 2, N'TAI', CONVERT(DATETIME, '2026-10-21T21:05:00', 126));

IF NOT EXISTS (SELECT 1 FROM dbo.lich_su_tai_lieu WHERE user_id = 4 AND tai_lieu_id = 4 AND hanh_dong = N'TAI')
    INSERT INTO dbo.lich_su_tai_lieu (user_id, tai_lieu_id, hanh_dong, thoi_gian)
    VALUES (4, 4, N'TAI', CONVERT(DATETIME, '2026-10-15T08:25:00', 126));

IF NOT EXISTS (SELECT 1 FROM dbo.lich_su_tai_lieu WHERE user_id = 4 AND tai_lieu_id = 7 AND hanh_dong = N'XEM')
    INSERT INTO dbo.lich_su_tai_lieu (user_id, tai_lieu_id, hanh_dong, thoi_gian)
    VALUES (4, 7, N'XEM', CONVERT(DATETIME, '2026-10-17T15:00:00', 126));

IF NOT EXISTS (SELECT 1 FROM dbo.lich_su_tai_lieu WHERE user_id = 4 AND tai_lieu_id = 12 AND hanh_dong = N'XEM')
    INSERT INTO dbo.lich_su_tai_lieu (user_id, tai_lieu_id, hanh_dong, thoi_gian)
    VALUES (4, 12, N'XEM', CONVERT(DATETIME, '2026-10-18T19:30:00', 126));

IF NOT EXISTS (SELECT 1 FROM dbo.lich_su_tai_lieu WHERE user_id = 5 AND tai_lieu_id = 10 AND hanh_dong = N'XEM')
    INSERT INTO dbo.lich_su_tai_lieu (user_id, tai_lieu_id, hanh_dong, thoi_gian)
    VALUES (5, 10, N'XEM', CONVERT(DATETIME, '2026-10-16T09:15:00', 126));

IF NOT EXISTS (SELECT 1 FROM dbo.lich_su_tai_lieu WHERE user_id = 5 AND tai_lieu_id = 10 AND hanh_dong = N'TAI')
    INSERT INTO dbo.lich_su_tai_lieu (user_id, tai_lieu_id, hanh_dong, thoi_gian)
    VALUES (5, 10, N'TAI', CONVERT(DATETIME, '2026-10-16T09:20:00', 126));

IF NOT EXISTS (SELECT 1 FROM dbo.lich_su_tai_lieu WHERE user_id = 5 AND tai_lieu_id = 21 AND hanh_dong = N'TAI')
    INSERT INTO dbo.lich_su_tai_lieu (user_id, tai_lieu_id, hanh_dong, thoi_gian)
    VALUES (5, 21, N'TAI', CONVERT(DATETIME, '2026-10-19T16:20:00', 126));

IF NOT EXISTS (SELECT 1 FROM dbo.lich_su_tai_lieu WHERE user_id = 6 AND tai_lieu_id = 11 AND hanh_dong = N'TAI')
    INSERT INTO dbo.lich_su_tai_lieu (user_id, tai_lieu_id, hanh_dong, thoi_gian)
    VALUES (6, 11, N'TAI', CONVERT(DATETIME, '2026-10-18T11:00:00', 126));

IF NOT EXISTS (SELECT 1 FROM dbo.lich_su_tai_lieu WHERE user_id = 6 AND tai_lieu_id = 19 AND hanh_dong = N'XEM')
    INSERT INTO dbo.lich_su_tai_lieu (user_id, tai_lieu_id, hanh_dong, thoi_gian)
    VALUES (6, 19, N'XEM', CONVERT(DATETIME, '2026-10-20T20:40:00', 126));

IF NOT EXISTS (SELECT 1 FROM dbo.lich_su_tai_lieu WHERE user_id = 8 AND tai_lieu_id = 23 AND hanh_dong = N'TAI')
    INSERT INTO dbo.lich_su_tai_lieu (user_id, tai_lieu_id, hanh_dong, thoi_gian)
    VALUES (8, 23, N'TAI', CONVERT(DATETIME, '2026-10-20T09:50:00', 126));

IF NOT EXISTS (SELECT 1 FROM dbo.lich_su_tai_lieu WHERE user_id = 8 AND tai_lieu_id = 22 AND hanh_dong = N'XEM')
    INSERT INTO dbo.lich_su_tai_lieu (user_id, tai_lieu_id, hanh_dong, thoi_gian)
    VALUES (8, 22, N'XEM', CONVERT(DATETIME, '2026-10-21T18:05:00', 126));
GO

/* 15.8. NHAT KY HOAT DONG BO SUNG */
IF NOT EXISTS (SELECT 1 FROM dbo.nhat_ky_hoat_dong WHERE id = 4)
    INSERT INTO dbo.nhat_ky_hoat_dong (id, user_id, hanh_dong, thoi_gian)
    VALUES (4, 4, N'Đăng ký tài khoản người đọc', CONVERT(DATETIME, '2026-10-14T19:50:00', 126));

IF NOT EXISTS (SELECT 1 FROM dbo.nhat_ky_hoat_dong WHERE id = 5)
    INSERT INTO dbo.nhat_ky_hoat_dong (id, user_id, hanh_dong, thoi_gian)
    VALUES (5, 4, N'Đăng nhập hệ thống', CONVERT(DATETIME, '2026-10-15T08:10:00', 126));

IF NOT EXISTS (SELECT 1 FROM dbo.nhat_ky_hoat_dong WHERE id = 6)
    INSERT INTO dbo.nhat_ky_hoat_dong (id, user_id, hanh_dong, thoi_gian)
    VALUES (6, 5, N'Đăng nhập hệ thống', CONVERT(DATETIME, '2026-10-16T08:55:00', 126));

IF NOT EXISTS (SELECT 1 FROM dbo.nhat_ky_hoat_dong WHERE id = 7)
    INSERT INTO dbo.nhat_ky_hoat_dong (id, user_id, hanh_dong, thoi_gian)
    VALUES (7, 6, N'Cập nhật thông tin cá nhân', CONVERT(DATETIME, '2026-10-18T10:20:00', 126));

IF NOT EXISTS (SELECT 1 FROM dbo.nhat_ky_hoat_dong WHERE id = 8)
    INSERT INTO dbo.nhat_ky_hoat_dong (id, user_id, hanh_dong, thoi_gian)
    VALUES (8, 6, N'Đổi mật khẩu tài khoản', CONVERT(DATETIME, '2026-10-18T10:25:00', 126));

IF NOT EXISTS (SELECT 1 FROM dbo.nhat_ky_hoat_dong WHERE id = 9)
    INSERT INTO dbo.nhat_ky_hoat_dong (id, user_id, hanh_dong, thoi_gian)
    VALUES (9, 8, N'Đăng nhập hệ thống', CONVERT(DATETIME, '2026-10-20T09:30:00', 126));

IF NOT EXISTS (SELECT 1 FROM dbo.nhat_ky_hoat_dong WHERE id = 10)
    INSERT INTO dbo.nhat_ky_hoat_dong (id, user_id, hanh_dong, thoi_gian)
    VALUES (10, 2, N'Thêm mới tài liệu Trí tuệ nhân tạo nhập môn', CONVERT(DATETIME, '2026-10-16T08:00:00', 126));
GO

/* 15.9. KIEM TRA NHANH SAU KHI BO SUNG DU LIEU */
SELECT N'nguoi_dung' AS Bang, COUNT(*) AS SoDong FROM dbo.nguoi_dung
UNION ALL SELECT N'the_loai', COUNT(*) FROM dbo.the_loai
UNION ALL SELECT N'tac_gia', COUNT(*) FROM dbo.tac_gia
UNION ALL SELECT N'tai_lieu (hoat dong)', COUNT(*) FROM dbo.tai_lieu WHERE trang_thai = N'HOAT_DONG'
UNION ALL SELECT N'giao_dich', COUNT(*) FROM dbo.giao_dich
UNION ALL SELECT N'quyen_tai_lieu', COUNT(*) FROM dbo.quyen_tai_lieu
UNION ALL SELECT N'lich_su_tai_lieu', COUNT(*) FROM dbo.lich_su_tai_lieu
UNION ALL SELECT N'nhat_ky_hoat_dong', COUNT(*) FROM dbo.nhat_ky_hoat_dong;
GO

-- Tai lieu tra phi cua nguoi doc 1 sau khi bo sung du lieu mau
SELECT tl.id, tl.ten_tai_lieu, tl.gia_ban, dbo.fn_CoQuyenTaiTaiLieu(1, tl.id) AS CoQuyenTai
FROM dbo.tai_lieu AS tl
WHERE tl.trang_thai = N'HOAT_DONG'
  AND tl.gia_ban > 0
ORDER BY tl.id;
GO

