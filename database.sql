USE [focustronic]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AlkatronicMeasurements](
	[AlkatronicMeasurementID] [int] IDENTITY(1,1) NOT NULL,
	[type] [nvarchar](50) NOT NULL,
	[kh_value] [int] NOT NULL,
	[ph_value] [int] NOT NULL,
	[solution_added] [int] NOT NULL,
	[acid_used] [int] NOT NULL,
	[is_power_plug_on] [bit] NOT NULL,
	[indicator] [int] NOT NULL,
	[is_hidden] [bit] NOT NULL,
	[note] [nvarchar](50) NULL,
	[record_time] [bigint] NOT NULL,
	[create_time] [bigint] NOT NULL
) ON [PRIMARY]
GO


SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Measurements](
	[MeasurementID] [int] IDENTITY(1,1) NOT NULL,
	[parameter] [nvarchar](50) NOT NULL,
	[value] [int] NOT NULL,
	[upper_bound] [int] NOT NULL,
	[lower_bound] [int] NOT NULL,
	[baselined_value] [int] NOT NULL,
	[multiply_factor] [int] NOT NULL,
	[indicator] [int] NOT NULL,
	[record_time] [bigint] NOT NULL,
 CONSTRAINT [PK_Measurements] PRIMARY KEY CLUSTERED 
(
	[MeasurementID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO