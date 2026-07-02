-- Dimensions-Tabellen

DROP TABLE IF EXISTS mart.dim_date;
GO
CREATE TABLE mart.dim_date (
    date_key     INT          NOT NULL,
    full_date    DATE         NOT NULL,
    year         SMALLINT     NOT NULL,
    quarter      TINYINT      NOT NULL,
    quarter_name NVARCHAR(3)  NOT NULL,
    month        TINYINT      NOT NULL,
    month_name   NVARCHAR(20) NOT NULL,
    month_short  NVARCHAR(5)  NOT NULL,
    week         TINYINT      NOT NULL,
    day          TINYINT      NOT NULL,
    weekday_name NVARCHAR(20) NOT NULL,
    is_weekend   BIT          NOT NULL,
    year_month   NVARCHAR(7)  NOT NULL,
    CONSTRAINT PK_dim_date PRIMARY KEY CLUSTERED (date_key)
);
GO
CREATE UNIQUE INDEX UX_dim_date_full ON mart.dim_date (full_date);
GO

DROP TABLE IF EXISTS mart.dim_account;
GO
CREATE TABLE mart.dim_account (
    account_key      INT           NOT NULL,
    account_id       CHAR(4)       NOT NULL,
    account_name     NVARCHAR(100) NOT NULL,
    account_category NVARCHAR(50)  NOT NULL,
    pl_line          NVARCHAR(20)  NOT NULL,
    pl_sort          TINYINT       NOT NULL,
    sign             SMALLINT      NOT NULL,
    CONSTRAINT PK_dim_account PRIMARY KEY CLUSTERED (account_key)
);
GO
CREATE UNIQUE INDEX UX_dim_account_id ON mart.dim_account (account_id);
GO

DROP TABLE IF EXISTS mart.dim_costcenter;
GO
CREATE TABLE mart.dim_costcenter (
    costcenter_key  INT           NOT NULL,
    costcenter_id   NVARCHAR(10)  NOT NULL,
    costcenter_name NVARCHAR(100) NOT NULL,
    area            NVARCHAR(50)  NOT NULL,
    cost_owner_id   NVARCHAR(10)  NOT NULL,
    CONSTRAINT PK_dim_costcenter PRIMARY KEY CLUSTERED (costcenter_key)
);
GO
CREATE UNIQUE INDEX UX_dim_costcenter_id ON mart.dim_costcenter (costcenter_id);
GO

DROP TABLE IF EXISTS mart.dim_product;
GO
CREATE TABLE mart.dim_product (
    product_key   INT           NOT NULL,
    product_id    NVARCHAR(10)  NOT NULL,
    product_name  NVARCHAR(100) NOT NULL,
    product_type  NVARCHAR(20)  NOT NULL,
    margin_class  NVARCHAR(20)  NOT NULL,
    CONSTRAINT PK_dim_product PRIMARY KEY CLUSTERED (product_key)
);
GO
CREATE UNIQUE INDEX UX_dim_product_id ON mart.dim_product (product_id);
GO

DROP TABLE IF EXISTS mart.dim_scenario;
GO
CREATE TABLE mart.dim_scenario (
    scenario_key   INT          NOT NULL,
    scenario_id    NVARCHAR(10) NOT NULL,
    scenario_label NVARCHAR(20) NOT NULL,
    scenario_order TINYINT      NOT NULL,
    CONSTRAINT PK_dim_scenario PRIMARY KEY CLUSTERED (scenario_key)
);
GO

DROP TABLE IF EXISTS mart.dim_cost_type;
GO
CREATE TABLE mart.dim_cost_type (
    cost_type_key   INT          NOT NULL,
    cost_type_id    NVARCHAR(10) NOT NULL,
    cost_type_label NVARCHAR(30) NOT NULL,
    cost_type_order TINYINT      NOT NULL,
    CONSTRAINT PK_dim_cost_type PRIMARY KEY CLUSTERED (cost_type_key)
);
GO
