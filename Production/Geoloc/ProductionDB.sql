-- --------------------------------------------------------
-- Host:                         server8.locanix.net
-- Server version:               5.7.19-log - MySQL Community Server (GPL)
-- Server OS:                    Win64
-- HeidiSQL Version:             9.4.0.5125
-- --------------------------------------------------------

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET NAMES utf8 */;
/*!50503 SET NAMES utf8mb4 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;


-- Dumping database structure for geoloc
CREATE DATABASE IF NOT EXISTS `geoloc` /*!40100 DEFAULT CHARACTER SET utf8 */;
USE `geoloc`;

-- Dumping structure for table geoloc.actions
CREATE TABLE IF NOT EXISTS `actions` (
  `ID` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `TrackerID` int(10) unsigned NOT NULL,
  `ActionType` varchar(10) NOT NULL,
  `Name` varchar(20) NOT NULL,
  `Data` varchar(45) CHARACTER SET utf8 COLLATE utf8_bin NOT NULL,
  PRIMARY KEY (`ID`),
  KEY `FK_Actions_1` (`TrackerID`),
  CONSTRAINT `FK_Actions_1` FOREIGN KEY (`TrackerID`) REFERENCES `trackers` (`ID`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Data exporting was unselected.
-- Dumping structure for table geoloc.commands
CREATE TABLE IF NOT EXISTS `commands` (
  `ID` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `TrackerID` int(10) unsigned NOT NULL,
  `Time` int(10) unsigned NOT NULL,
  `Command` varchar(255) NOT NULL DEFAULT '',
  `Param` varchar(255) NOT NULL DEFAULT '',
  PRIMARY KEY (`ID`),
  KEY `FK_commands_1` (`TrackerID`),
  CONSTRAINT `FK_commands_1` FOREIGN KEY (`TrackerID`) REFERENCES `trackers` (`ID`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Data exporting was unselected.
-- Dumping structure for table geoloc.commandtemplates
CREATE TABLE IF NOT EXISTS `commandtemplates` (
  `ID` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `UserID` int(10) unsigned NOT NULL,
  `Name` varchar(128) NOT NULL DEFAULT 'New',
  `Command` varchar(255) NOT NULL DEFAULT '',
  PRIMARY KEY (`ID`),
  KEY `FK_commandtemplates_1` (`UserID`) USING BTREE,
  CONSTRAINT `FK_commandtemplates_1` FOREIGN KEY (`UserID`) REFERENCES `users` (`ID`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 ROW_FORMAT=COMPACT;

-- Data exporting was unselected.
-- Dumping structure for table geoloc.curpos
CREATE TABLE IF NOT EXISTS `curpos` (
  `TrackerID` int(10) unsigned NOT NULL COMMENT 'номер трекера',
  `Time` int(10) unsigned NOT NULL DEFAULT '0',
  `Lng` float(9,6) NOT NULL DEFAULT '0.000000' COMMENT 'долгота, в градусах. "+" - восточная долгота, "-" - западная',
  `Lat` float(9,6) NOT NULL DEFAULT '0.000000' COMMENT 'широта, в градусах.  "+" - северная широта, "-" - южная',
  `Status` int(10) unsigned NOT NULL DEFAULT '0' COMMENT 'Статус в виде десятичного числа sfhhhhhnn,\r\nгде s - тип отчета, f - 2D или 3D, hhh.hh - направление, nn - количество спутников',
  `Speed` smallint(5) unsigned NOT NULL DEFAULT '0' COMMENT 'скорость в км/ч',
  `Alt` smallint(6) NOT NULL DEFAULT '0' COMMENT 'высота над уровнем моря в метрах',
  `IO` blob,
  `online` tinyint(1) NOT NULL DEFAULT '0',
  `GSMInfo` bigint(20) unsigned NOT NULL DEFAULT '0',
  PRIMARY KEY (`TrackerID`) USING BTREE,
  CONSTRAINT `FK_curpos_1` FOREIGN KEY (`TrackerID`) REFERENCES `trackers` (`ID`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Data exporting was unselected.
-- Dumping structure for table geoloc.curtrackerstat
CREATE TABLE IF NOT EXISTS `curtrackerstat` (
  `TrackerID` int(10) unsigned NOT NULL,
  `EngineTime` int(10) unsigned NOT NULL DEFAULT '0',
  `EngineSyncDay` int(10) unsigned NOT NULL DEFAULT '0',
  `Length` float NOT NULL DEFAULT '0',
  `LengthSyncDay` int(10) unsigned NOT NULL DEFAULT '0',
  PRIMARY KEY (`TrackerID`),
  CONSTRAINT `FK_CurTrackerStat_1` FOREIGN KEY (`TrackerID`) REFERENCES `trackers` (`ID`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 ROW_FORMAT=COMPACT;

-- Data exporting was unselected.
-- Dumping structure for table geoloc.dailystat
CREATE TABLE IF NOT EXISTS `dailystat` (
  `TrackerID` int(10) unsigned NOT NULL,
  `Day` int(10) unsigned NOT NULL,
  `StartLat` float NOT NULL DEFAULT '0',
  `StartLng` float NOT NULL DEFAULT '0',
  `EndLat` float NOT NULL DEFAULT '0',
  `EndLng` float NOT NULL DEFAULT '0',
  `Length` float NOT NULL DEFAULT '0',
  `EngineTime` int(10) unsigned NOT NULL DEFAULT '0',
  `MotoTime` int(10) unsigned NOT NULL DEFAULT '0',
  `ParkTime` int(10) unsigned NOT NULL DEFAULT '0',
  `MoveTime` int(10) unsigned NOT NULL DEFAULT '0',
  `IdleTime` int(10) unsigned NOT NULL DEFAULT '0',
  `AvgSpeed` int(10) unsigned NOT NULL DEFAULT '0',
  `MaxSpeed` int(10) unsigned NOT NULL DEFAULT '0',
  `Fuel` float NOT NULL DEFAULT '0',
  `Fuelling` float NOT NULL DEFAULT '0',
  `Drain` float NOT NULL DEFAULT '0',
  `FuelPerKm` float NOT NULL DEFAULT '0',
  `FuelPerH` float NOT NULL DEFAULT '0',
  `Status` int(10) unsigned NOT NULL DEFAULT '0',
  `Mileage` double unsigned NOT NULL DEFAULT '0',
  `TotalEngineTime` int(10) unsigned NOT NULL DEFAULT '0',
  `Mask` int(10) unsigned NOT NULL DEFAULT '0',
  PRIMARY KEY (`TrackerID`,`Day`),
  KEY `Index_2` (`Day`,`Status`) USING BTREE,
  CONSTRAINT `FK_DailyStat_1` FOREIGN KEY (`TrackerID`) REFERENCES `trackers` (`ID`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 ROW_FORMAT=COMPACT;

-- Data exporting was unselected.
-- Dumping structure for table geoloc.driver2groups
CREATE TABLE IF NOT EXISTS `driver2groups` (
  `DriverID` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `GroupID` int(10) unsigned NOT NULL,
  PRIMARY KEY (`DriverID`,`GroupID`),
  KEY `FK_driver2groups_2` (`GroupID`) USING BTREE,
  CONSTRAINT `FK_driver2groups_1` FOREIGN KEY (`DriverID`) REFERENCES `drivers` (`ID`) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT `FK_driver2groups_2` FOREIGN KEY (`GroupID`) REFERENCES `drivergroups` (`ID`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 ROW_FORMAT=COMPACT;

-- Data exporting was unselected.
-- Dumping structure for table geoloc.drivergroups
CREATE TABLE IF NOT EXISTS `drivergroups` (
  `ID` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `UserID` int(10) unsigned NOT NULL,
  `Name` varchar(128) NOT NULL DEFAULT 'New',
  PRIMARY KEY (`ID`),
  KEY `FK_driversgroups_1` (`UserID`) USING BTREE,
  CONSTRAINT `FK_driversgroups_1` FOREIGN KEY (`UserID`) REFERENCES `users` (`ID`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 ROW_FORMAT=COMPACT;

-- Data exporting was unselected.
-- Dumping structure for table geoloc.drivers
CREATE TABLE IF NOT EXISTS `drivers` (
  `ID` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `UserID` int(10) unsigned NOT NULL,
  `Name` varchar(128) NOT NULL DEFAULT 'New',
  `Phone` varchar(45) NOT NULL DEFAULT '',
  `DriverLic` varchar(128) NOT NULL DEFAULT '',
  `Category` varchar(45) NOT NULL DEFAULT '',
  `Class` varchar(45) NOT NULL DEFAULT '',
  PRIMARY KEY (`ID`),
  KEY `FK_Driver_1` (`UserID`) USING BTREE,
  CONSTRAINT `FK_Driver_1` FOREIGN KEY (`UserID`) REFERENCES `users` (`ID`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 ROW_FORMAT=COMPACT;

-- Data exporting was unselected.
-- Dumping structure for table geoloc.driversessions
CREATE TABLE IF NOT EXISTS `driversessions` (
  `DriverID` int(10) unsigned NOT NULL,
  `TrackerID` int(10) unsigned NOT NULL,
  `TimeFrom` int(10) unsigned NOT NULL,
  `TimeTill` int(10) unsigned NOT NULL DEFAULT '0',
  `Status` int(10) unsigned NOT NULL DEFAULT '0',
  `Length` float NOT NULL DEFAULT '0',
  `EngineTime` int(10) unsigned NOT NULL DEFAULT '0',
  `IdleTime` int(10) unsigned NOT NULL DEFAULT '0',
  `AvgSpeed` int(10) unsigned NOT NULL DEFAULT '0',
  `MaxSpeed` int(10) unsigned NOT NULL DEFAULT '0',
  `Fuel` float NOT NULL DEFAULT '0',
  `ViolationCount` int(10) unsigned NOT NULL DEFAULT '0',
  `DriverPenality` int(10) unsigned NOT NULL DEFAULT '0',
  `ViolationDuration` int(10) unsigned NOT NULL DEFAULT '0',
  PRIMARY KEY (`DriverID`,`TrackerID`,`TimeFrom`),
  KEY `FK_driversession_2` (`TrackerID`) USING BTREE,
  CONSTRAINT `FK_driversession_1` FOREIGN KEY (`DriverID`) REFERENCES `drivers` (`ID`) ON DELETE CASCADE,
  CONSTRAINT `FK_driversession_2` FOREIGN KEY (`TrackerID`) REFERENCES `trackers` (`ID`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 ROW_FORMAT=COMPACT;

-- Data exporting was unselected.
-- Dumping structure for table geoloc.ekeys
CREATE TABLE IF NOT EXISTS `ekeys` (
  `ID` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `UserID` int(10) unsigned NOT NULL,
  `EKey` varchar(45) NOT NULL DEFAULT '',
  `Name` varchar(100) NOT NULL DEFAULT 'Key',
  `PinCode` varchar(45) NOT NULL DEFAULT '',
  `DriverID` int(10) unsigned DEFAULT NULL,
  `TrackerID` int(10) unsigned DEFAULT NULL,
  `Type` varchar(45) NOT NULL DEFAULT 'iButton',
  `Limit` int(10) unsigned NOT NULL DEFAULT '0',
  `LimitType` int(10) unsigned NOT NULL DEFAULT '0',
  `OneTime` tinyint(1) NOT NULL DEFAULT '0',
  `Comment` varchar(100) NOT NULL DEFAULT '',
  PRIMARY KEY (`ID`),
  UNIQUE KEY `Index_5` (`UserID`,`EKey`) USING BTREE,
  KEY `FK_tags_1` (`UserID`) USING BTREE,
  KEY `FK_ekeys_2` (`DriverID`) USING BTREE,
  KEY `FK_ekeys_3` (`TrackerID`) USING BTREE,
  CONSTRAINT `FK_ekeys_2` FOREIGN KEY (`DriverID`) REFERENCES `drivers` (`ID`) ON DELETE SET NULL ON UPDATE CASCADE,
  CONSTRAINT `FK_ekeys_3` FOREIGN KEY (`TrackerID`) REFERENCES `trackers` (`ID`) ON DELETE SET NULL ON UPDATE CASCADE,
  CONSTRAINT `FK_tags_1` FOREIGN KEY (`UserID`) REFERENCES `users` (`ID`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Data exporting was unselected.
-- Dumping structure for table geoloc.ekeys2trackers
CREATE TABLE IF NOT EXISTS `ekeys2trackers` (
  `TrackerID` int(10) unsigned NOT NULL,
  `EKeyID` int(10) unsigned NOT NULL,
  `NeedAdd` tinyint(1) NOT NULL DEFAULT '1',
  `NeedDelete` tinyint(1) NOT NULL DEFAULT '0',
  `NeedUpdate` tinyint(1) NOT NULL DEFAULT '0',
  PRIMARY KEY (`TrackerID`,`EKeyID`) USING BTREE,
  KEY `FK_ekeys2trackers_2` (`EKeyID`) USING BTREE,
  CONSTRAINT `FK_ekeys2trackers_1` FOREIGN KEY (`TrackerID`) REFERENCES `trackers` (`ID`) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT `FK_ekeys2trackers_2` FOREIGN KEY (`EKeyID`) REFERENCES `ekeys` (`ID`) ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Data exporting was unselected.
-- Dumping structure for table geoloc.email
CREATE TABLE IF NOT EXISTS `email` (
  `ID` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `Address` varchar(250) NOT NULL DEFAULT '',
  `Text` varchar(255) NOT NULL DEFAULT '',
  `RetryCnt` int(10) unsigned NOT NULL DEFAULT '0',
  `RetryTime` int(10) unsigned NOT NULL DEFAULT '0',
  PRIMARY KEY (`ID`),
  KEY `Index_2` (`RetryTime`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Data exporting was unselected.
-- Dumping structure for table geoloc.events
CREATE TABLE IF NOT EXISTS `events` (
  `TrackerID` int(10) unsigned NOT NULL,
  `Time` int(10) unsigned NOT NULL,
  `Type` int(10) unsigned NOT NULL,
  `Status` int(10) unsigned NOT NULL DEFAULT '0',
  `Text` varchar(255) NOT NULL DEFAULT '0',
  PRIMARY KEY (`TrackerID`,`Time`,`Type`),
  KEY `Index_2` (`Status`) USING BTREE,
  CONSTRAINT `FK_events_1` FOREIGN KEY (`TrackerID`) REFERENCES `trackers` (`ID`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Data exporting was unselected.
-- Dumping structure for table geoloc.eventsettings
CREATE TABLE IF NOT EXISTS `eventsettings` (
  `ID` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `TrackerID` int(10) unsigned NOT NULL,
  `Events` varchar(250) NOT NULL DEFAULT 'ALL',
  `ActiveFrom` int(10) unsigned NOT NULL DEFAULT '0',
  `ActiveTill` int(10) unsigned NOT NULL DEFAULT '86400',
  `Phone` varchar(50) NOT NULL DEFAULT '',
  `Email` varchar(250) NOT NULL DEFAULT '',
  `Name` varchar(150) NOT NULL DEFAULT '',
  PRIMARY KEY (`ID`),
  KEY `FK_eventsettings_1` (`TrackerID`),
  CONSTRAINT `FK_eventsettings_1` FOREIGN KEY (`TrackerID`) REFERENCES `trackers` (`ID`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Data exporting was unselected.
-- Dumping structure for event geoloc.evtSetTrackersLastLocation
DELIMITER //
CREATE DEFINER=`geoloc`@`%` EVENT `evtSetTrackersLastLocation` ON SCHEDULE EVERY 2 SECOND STARTS '2016-09-30 23:03:35' ON COMPLETION NOT PRESERVE ENABLE DO CALL spSetTrackersLastLocation()//
DELIMITER ;

-- Dumping structure for table geoloc.fueltransactions
CREATE TABLE IF NOT EXISTS `fueltransactions` (
  `TrackerID` int(10) unsigned NOT NULL,
  `Time` int(10) unsigned NOT NULL,
  `InnerID` int(10) unsigned NOT NULL DEFAULT '0',
  `EKey` bigint(20) unsigned NOT NULL DEFAULT '0',
  `FinishTime` int(10) unsigned NOT NULL DEFAULT '0',
  `AuthType` int(10) unsigned NOT NULL DEFAULT '0',
  `DriverState` int(10) unsigned NOT NULL DEFAULT '0',
  `PumpID` int(10) unsigned NOT NULL DEFAULT '0',
  `Value` float NOT NULL DEFAULT '0',
  `NValue` int(10) unsigned NOT NULL DEFAULT '0',
  `Total` float NOT NULL DEFAULT '0',
  `VehicleId` varchar(45) NOT NULL DEFAULT '',
  `Odometr` int(10) unsigned NOT NULL DEFAULT '0',
  `FuelVolume1` float NOT NULL DEFAULT '0',
  `FuelVolume2` float NOT NULL DEFAULT '0',
  `TagLimit` float NOT NULL DEFAULT '0',
  `Lat` float NOT NULL DEFAULT '0',
  `Lng` float NOT NULL DEFAULT '0',
  PRIMARY KEY (`TrackerID`,`Time`),
  CONSTRAINT `FK_fueltransactions_1` FOREIGN KEY (`TrackerID`) REFERENCES `trackers` (`ID`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Data exporting was unselected.
-- Dumping structure for table geoloc.gsminfo
CREATE TABLE IF NOT EXISTS `gsminfo` (
  `ID` bigint(20) unsigned NOT NULL,
  `Lat` float NOT NULL DEFAULT '0',
  `Lng` float NOT NULL DEFAULT '0',
  `Radius` int(10) unsigned NOT NULL DEFAULT '1000',
  `Time` int(10) unsigned NOT NULL DEFAULT '0',
  PRIMARY KEY (`ID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Data exporting was unselected.
-- Dumping structure for table geoloc.icons
CREATE TABLE IF NOT EXISTS `icons` (
  `ID` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `width` int(10) unsigned NOT NULL DEFAULT '32',
  `height` int(10) unsigned NOT NULL DEFAULT '32',
  `anchorx` int(10) unsigned NOT NULL DEFAULT '16',
  `anchory` int(10) unsigned NOT NULL DEFAULT '16',
  `url` varchar(256) NOT NULL DEFAULT '',
  `url_cross` varchar(256) NOT NULL DEFAULT '',
  `url_disabled` varchar(256) NOT NULL DEFAULT '',
  `color` varchar(10) NOT NULL DEFAULT '',
  `Name` varchar(45) NOT NULL DEFAULT '',
  `Rotate` tinyint(1) NOT NULL DEFAULT '0',
  PRIMARY KEY (`ID`)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8 COMMENT='icons';

-- Data exporting was unselected.
-- Dumping structure for table geoloc.lastekeys
CREATE TABLE IF NOT EXISTS `lastekeys` (
  `TrackerID` int(10) unsigned NOT NULL,
  `Time` int(10) unsigned NOT NULL,
  `EKey` bigint(20) unsigned NOT NULL,
  PRIMARY KEY (`TrackerID`),
  CONSTRAINT `FK_lastekeys_1` FOREIGN KEY (`TrackerID`) REFERENCES `trackers` (`ID`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 ROW_FORMAT=COMPACT;

-- Data exporting was unselected.
-- Dumping structure for table geoloc.lastpoint
CREATE TABLE IF NOT EXISTS `lastpoint` (
  `TrackerID` int(10) unsigned NOT NULL,
  `Time` int(10) unsigned NOT NULL DEFAULT '0',
  `Lat` float NOT NULL DEFAULT '0',
  `Lng` float NOT NULL DEFAULT '0',
  `Status` int(10) unsigned NOT NULL DEFAULT '0',
  `Speed` smallint(5) unsigned NOT NULL DEFAULT '0',
  `Alt` smallint(6) NOT NULL DEFAULT '0',
  `IO` blob,
  `GSMInfo` bigint(20) unsigned NOT NULL DEFAULT '0',
  PRIMARY KEY (`TrackerID`) USING BTREE,
  CONSTRAINT `FK_lastpoint_1` FOREIGN KEY (`TrackerID`) REFERENCES `trackers` (`ID`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Data exporting was unselected.
-- Dumping structure for event geoloc.LastWeek Deletion
DELIMITER //
CREATE DEFINER=`root`@`localhost` EVENT `LastWeek Deletion` ON SCHEDULE EVERY 1 WEEK STARTS '2017-09-22 00:00:00' ON COMPLETION NOT PRESERVE ENABLE COMMENT 'delete historical data at every week' DO BEGIN
delete from points where ForwardTime<=(UNIX_TIMESTAMP(NOW())-(3600*24*14)) and ReceivedTime<=(UNIX_TIMESTAMP(NOW())-(3600*24*14));
END//
DELIMITER ;

-- Dumping structure for function geoloc.ParseAddress
DELIMITER //
CREATE DEFINER=`geoloc`@`%` FUNCTION `ParseAddress`(
	`address` LONGTEXT,
	`tolerance` DECIMAL(10,2)
) RETURNS longtext CHARSET utf8
    NO SQL
BEGIN
set @str:=address;
set @tol:=tolerance;
select SUBSTRING_INDEX(SUBSTRING_INDEX(@str, ' ', 1), ' ', -1) into @value ;
SELECT IF( (@value <= @tol) && (POSITION('of' IN @str) > 0) && (POSITION('km' IN @str) > 0),
REPLACE(@str,left(@str, POSITION('of' IN @str)+2),''),
@str
)into @plantname;

return REPLACE(@plantname,',','|')   ;

END//
DELIMITER ;

-- Dumping structure for table geoloc.photos
CREATE TABLE IF NOT EXISTS `photos` (
  `TrackerID` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `Time` int(10) unsigned NOT NULL,
  `Buffer` mediumblob NOT NULL,
  PRIMARY KEY (`TrackerID`,`Time`),
  CONSTRAINT `FK_photos_1` FOREIGN KEY (`TrackerID`) REFERENCES `trackers` (`ID`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 ROW_FORMAT=COMPACT;

-- Data exporting was unselected.
-- Dumping structure for table geoloc.points
CREATE TABLE IF NOT EXISTS `points` (
  `TrackerID` int(10) unsigned NOT NULL COMMENT 'номер трекера',
  `Time` int(11) unsigned NOT NULL COMMENT 'время',
  `Lng` float(9,6) NOT NULL COMMENT 'долгота, в градусах. "+" - восточная долгота, "-" - западная',
  `Lat` float(9,6) NOT NULL COMMENT 'широта, в градусах.  "+" - северная широта, "-" - южная',
  `Status` int(10) unsigned NOT NULL COMMENT 'Статус в виде десятичного числа sfhhhhhnn,\r\nгде s - тип отчета, f - 2D или 3D, hhh.hh - направление, nn - количество спутников',
  `speed` smallint(5) unsigned NOT NULL DEFAULT '0' COMMENT 'скорость в км/ч',
  `Alt` smallint(6) NOT NULL DEFAULT '0' COMMENT 'высота над уровнем моря в метрах',
  `IO` blob,
  `GSMInfo` bigint(20) unsigned NOT NULL DEFAULT '0',
  `Send` int(10) unsigned NOT NULL DEFAULT '0',
  `ForwardTime` int(11) DEFAULT NULL,
  `ReceivedTime` int(11) DEFAULT NULL,
  `NTS` int(11) NOT NULL DEFAULT '0' COMMENT 'packet No of time Sent',
  `VehicleStatus` int(3) DEFAULT '0' COMMENT '1 For Running and 0 For Standing',
  PRIMARY KEY (`TrackerID`,`Time`),
  KEY `Send` (`TrackerID`,`Send`),
  KEY `Forward and Received` (`ForwardTime`,`ReceivedTime`),
  CONSTRAINT `FK_points_1` FOREIGN KEY (`TrackerID`) REFERENCES `trackers` (`ID`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Data exporting was unselected.
-- Dumping structure for table geoloc.regions
CREATE TABLE IF NOT EXISTS `regions` (
  `ID` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `Name` varchar(100) NOT NULL DEFAULT '',
  PRIMARY KEY (`ID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 ROW_FORMAT=COMPACT;

-- Data exporting was unselected.
-- Dumping structure for table geoloc.routes
CREATE TABLE IF NOT EXISTS `routes` (
  `ID` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `UserID` int(10) unsigned NOT NULL,
  `Name` varchar(128) NOT NULL DEFAULT 'New',
  `Comment` varchar(256) NOT NULL DEFAULT '',
  PRIMARY KEY (`ID`),
  KEY `FK_Route_1` (`UserID`) USING BTREE,
  CONSTRAINT `FK_Route_1` FOREIGN KEY (`UserID`) REFERENCES `users` (`ID`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 ROW_FORMAT=COMPACT;

-- Data exporting was unselected.
-- Dumping structure for table geoloc.routes2zones
CREATE TABLE IF NOT EXISTS `routes2zones` (
  `RouteID` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `SortOrder` int(10) unsigned NOT NULL,
  `Time` int(10) unsigned NOT NULL DEFAULT '60',
  `ZoneID` int(10) unsigned NOT NULL DEFAULT '0',
  PRIMARY KEY (`RouteID`,`SortOrder`),
  KEY `FK_routes2zones_2` (`ZoneID`) USING BTREE,
  CONSTRAINT `FK_routes2zones_1 ` FOREIGN KEY (`RouteID`) REFERENCES `routes` (`ID`) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT `FK_routes2zones_2` FOREIGN KEY (`ZoneID`) REFERENCES `zones` (`ID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 ROW_FORMAT=COMPACT;

-- Data exporting was unselected.
-- Dumping structure for table geoloc.servicetask
CREATE TABLE IF NOT EXISTS `servicetask` (
  `ID` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `TrackerID` int(10) unsigned NOT NULL,
  `Name` varchar(128) NOT NULL DEFAULT '',
  `Comment` varchar(128) NOT NULL DEFAULT '',
  `TimeFrom` int(10) unsigned NOT NULL DEFAULT '0',
  `TimePeriod` int(10) NOT NULL DEFAULT '0',
  `MileageFrom` int(10) unsigned NOT NULL DEFAULT '0',
  `MileagePeriod` int(10) NOT NULL DEFAULT '0',
  `MotoFrom` int(10) unsigned NOT NULL DEFAULT '0',
  `MotoPeriod` int(10) NOT NULL DEFAULT '0',
  PRIMARY KEY (`ID`),
  KEY `FK_servicetask_1` (`TrackerID`) USING BTREE,
  CONSTRAINT `FK_servicetask_1` FOREIGN KEY (`TrackerID`) REFERENCES `trackers` (`ID`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 ROW_FORMAT=DYNAMIC;

-- Data exporting was unselected.
-- Dumping structure for table geoloc.servicetaskactions
CREATE TABLE IF NOT EXISTS `servicetaskactions` (
  `ID` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `TrackerID` int(10) unsigned NOT NULL,
  `Time` int(10) unsigned NOT NULL,
  `Comment` varchar(256) NOT NULL DEFAULT '',
  `Moto` int(10) unsigned NOT NULL DEFAULT '0',
  `Mileage` int(10) unsigned NOT NULL DEFAULT '0',
  `Price` float unsigned NOT NULL DEFAULT '0',
  `Duration` int(10) unsigned NOT NULL DEFAULT '0',
  `Address` varchar(128) NOT NULL DEFAULT '',
  PRIMARY KEY (`ID`),
  KEY `FK_sta_to_trackers_idx` (`TrackerID`) USING BTREE,
  CONSTRAINT `FK_sta_to_trackers` FOREIGN KEY (`TrackerID`) REFERENCES `trackers` (`ID`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 ROW_FORMAT=DYNAMIC;

-- Data exporting was unselected.
-- Dumping structure for table geoloc.servicetasks2actions
CREATE TABLE IF NOT EXISTS `servicetasks2actions` (
  `ActionID` int(11) unsigned NOT NULL,
  `ServiceTaskID` int(11) unsigned NOT NULL,
  PRIMARY KEY (`ActionID`,`ServiceTaskID`),
  KEY `fk_st2a_servicetasks_idx` (`ServiceTaskID`) USING BTREE,
  CONSTRAINT `fk_st2a_actions` FOREIGN KEY (`ActionID`) REFERENCES `servicetaskactions` (`ID`) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT `fk_st2a_servicetasks` FOREIGN KEY (`ServiceTaskID`) REFERENCES `servicetask` (`ID`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 ROW_FORMAT=DYNAMIC;

-- Data exporting was unselected.
-- Dumping structure for table geoloc.shifts
CREATE TABLE IF NOT EXISTS `shifts` (
  `ID` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `UserID` int(10) unsigned NOT NULL,
  `Name` varchar(128) NOT NULL DEFAULT 'New',
  `From` int(10) unsigned NOT NULL DEFAULT '28800',
  `Till` int(10) unsigned NOT NULL DEFAULT '61200',
  `Mask` int(10) unsigned NOT NULL DEFAULT '31',
  PRIMARY KEY (`ID`),
  KEY `FK_shifts_1` (`UserID`) USING BTREE,
  CONSTRAINT `FK_shifts_1` FOREIGN KEY (`UserID`) REFERENCES `users` (`ID`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 ROW_FORMAT=COMPACT;

-- Data exporting was unselected.
-- Dumping structure for table geoloc.sms
CREATE TABLE IF NOT EXISTS `sms` (
  `ID` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `Processed` tinyint(1) NOT NULL DEFAULT '0',
  `Address` varchar(45) NOT NULL DEFAULT '',
  `Text` varchar(250) NOT NULL DEFAULT '',
  PRIMARY KEY (`ID`),
  KEY `Index_2` (`Processed`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Data exporting was unselected.
-- Dumping structure for procedure geoloc.spSetTrackersLastLocation
DELIMITER //
CREATE DEFINER=`geoloc`@`%` PROCEDURE `spSetTrackersLastLocation`()
BEGIN

	DECLARE loopIndex INT DEFAULT 0;
	DECLARE var_Count INT DEFAULT 0;
	
	DECLARE var_TrackerID int(10);
	DECLARE var_IMEI bigint(20);
	DECLARE var_Name varchar(45);
	DECLARE var_Servergroup varchar(128);
	DECLARE var_Time int(10);
	DECLARE var_Lng float(9,6);
	DECLARE var_Lat float(9,6);
	DECLARE var_Status int(10);
	DECLARE var_Speed smallint(5);
	DECLARE var_Alt smallint(6);
	DECLARE var_GSMInfo bigint(20);
	DECLARE var_Send int(10);
	
   DECLARE curT CURSOR FOR SELECT t.ID, t.IMEI, t.Name, t.servergroup FROM trackers as t ORDER BY t.ID;
   /*DECLARE CONTINUE HANDLER FOR NOT FOUND SET loopDone = TRUE;*/

	DROP TEMPORARY TABLE IF EXISTS tblResults;
   /*CREATE TEMPORARY TABLE IF NOT EXISTS tblResults (
		tmp_TrackerID int(10),
		tmp_IMEI bigint(20),
		tmp_Name varchar(45),
		tmp_Servergroup varchar(128),
		tmp_Time int(10),
		tmp_Lng float(9,6),
		tmp_Lat float(9,6),
		tmp_Status int(10),
		tmp_Speed smallint(5),
		tmp_Alt smallint(6),
		tmp_GSMInfo bigint(20),
		tmp_Send int(10)
   );*/
   
   /*SELECT COUNT(t.ID) INTO var_Count from trackers as t;*/

   OPEN curT;
   SELECT FOUND_ROWS() INTO var_Count;
   
   read_loop: LOOP
   	FETCH curT INTO var_TrackerID, var_IMEI, var_Name, var_Servergroup;

    	SELECT 0, 0, 0, 0, 0, 0, 0, 0
			INTO var_Time, var_Lng, var_Lat, var_Status, var_Speed, var_Alt, var_GSMInfo, var_Send; 
    	
		SELECT pt.`Time`, pt.Lng, pt.Lat, pt.`Status`, pt.speed, pt.Alt, pt.`GSMInfo`, pt.Send
			INTO var_Time, var_Lng, var_Lat, var_Status, var_Speed, var_Alt, var_GSMInfo, var_Send  
			FROM points as pt
			WHERE pt.TrackerID = var_TrackerID
			ORDER BY pt.`Time` DESC
			LIMIT 1;
			
		IF (SELECT count(tl.TrackerID) FROM trackerlastlocation as tl WHERE tl.TrackerID = var_TrackerID) > 0 THEN
      	BEGIN
	      	UPDATE trackerlastlocation 
			  		SET IMEI = var_IMEI, Name = var_Name, Servergroup = var_Servergroup, `Time` = var_Time, 
					  		Lng = var_Lng, Lat = var_Lat, `Status` = var_Status, Speed = var_Speed, Alt = var_Alt, 
							`GSMInfo` = var_GSMInfo, Send = var_Send
					WHERE TrackerID = var_TrackerID;
        	END;
   	ELSE 
      	BEGIN
				INSERT INTO trackerlastlocation 
					VALUES (var_TrackerID, var_IMEI, var_Name, var_Servergroup, var_Time, var_Lng, var_Lat, 
								var_Status, var_Speed, var_Alt, var_GSMInfo, var_Send);
      	END;
      END IF;

		SET loopIndex := loopIndex + 1;
      IF loopIndex >= var_Count THEN
      	LEAVE read_loop;
		END IF;
		 
  	END LOOP;

  CLOSE curT;
  /*SELECT * FROM tblResults;*/

END//
DELIMITER ;

-- Dumping structure for table geoloc.tasks
CREATE TABLE IF NOT EXISTS `tasks` (
  `ID` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `Name` varchar(100) NOT NULL,
  `Comment` varchar(100) NOT NULL DEFAULT '',
  `TrackerID` int(10) unsigned NOT NULL,
  `Time` int(10) unsigned NOT NULL,
  `AheadMax` int(10) unsigned NOT NULL DEFAULT '30',
  `ZoneID` int(10) unsigned DEFAULT NULL,
  `Lat` float NOT NULL DEFAULT '0',
  `Lng` float NOT NULL DEFAULT '0',
  `Radius` int(10) unsigned NOT NULL DEFAULT '0',
  `Rep` int(10) unsigned NOT NULL DEFAULT '0',
  `RouteID` int(10) unsigned DEFAULT NULL,
  PRIMARY KEY (`ID`),
  KEY `FK_tasks_2` (`ZoneID`) USING BTREE,
  KEY `FK_tasks_1` (`TrackerID`,`Time`) USING BTREE,
  KEY `FK_tasks_3` (`RouteID`),
  CONSTRAINT `FK_tasks_1` FOREIGN KEY (`TrackerID`) REFERENCES `trackers` (`ID`) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT `FK_tasks_2` FOREIGN KEY (`ZoneID`) REFERENCES `zones` (`ID`) ON DELETE SET NULL ON UPDATE CASCADE,
  CONSTRAINT `FK_tasks_3` FOREIGN KEY (`RouteID`) REFERENCES `routes` (`ID`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 ROW_FORMAT=COMPACT;

-- Data exporting was unselected.
-- Dumping structure for table geoloc.timetable
CREATE TABLE IF NOT EXISTS `timetable` (
  `ID` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `UserID` int(10) unsigned NOT NULL,
  `Name` varchar(128) NOT NULL,
  `Year` int(10) unsigned NOT NULL,
  `Calendar` varchar(400) NOT NULL DEFAULT '',
  PRIMARY KEY (`ID`),
  KEY `FK_TimeTable_1` (`UserID`) USING BTREE,
  CONSTRAINT `FK_TimeTable_1` FOREIGN KEY (`UserID`) REFERENCES `users` (`ID`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 ROW_FORMAT=COMPACT;

-- Data exporting was unselected.
-- Dumping structure for event geoloc.TimeZoneControl
DELIMITER //
CREATE DEFINER=`geoloc`@`%` EVENT `TimeZoneControl` ON SCHEDULE EVERY 1 HOUR STARTS '2017-04-18 20:52:42' ON COMPLETION NOT PRESERVE ENABLE DO INSERT INTO Commands(TrackerID, Time, Command)
SELECT TrackerID, UNIX_TIMESTAMP(), "GMT,w,0,0#"
FROM LastPoint WHERE LastPoint.Time > UNIX_TIMESTAMP() + 3000//
DELIMITER ;

-- Dumping structure for table geoloc.trackergroups
CREATE TABLE IF NOT EXISTS `trackergroups` (
  `ID` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `UserID` int(10) unsigned NOT NULL,
  `Name` varchar(45) NOT NULL DEFAULT 'New group',
  PRIMARY KEY (`ID`),
  KEY `FK_trackergroups_1` (`UserID`),
  CONSTRAINT `FK_trackergroups_1` FOREIGN KEY (`UserID`) REFERENCES `users` (`ID`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Data exporting was unselected.
-- Dumping structure for table geoloc.trackerlastlocation
CREATE TABLE IF NOT EXISTS `trackerlastlocation` (
  `TrackerID` int(10) unsigned NOT NULL,
  `IMEI` bigint(20) NOT NULL,
  `Name` varchar(45) NOT NULL DEFAULT '',
  `Servergroup` varchar(128) NOT NULL DEFAULT '',
  `Time` int(10) unsigned NOT NULL,
  `Lng` float(9,6) NOT NULL,
  `Lat` float(9,6) NOT NULL,
  `Status` int(10) unsigned NOT NULL,
  `Speed` smallint(5) unsigned NOT NULL DEFAULT '0',
  `Alt` smallint(6) NOT NULL DEFAULT '0',
  `GSMInfo` bigint(20) unsigned NOT NULL DEFAULT '0',
  `Send` int(10) unsigned NOT NULL DEFAULT '0',
  PRIMARY KEY (`TrackerID`,`Time`),
  KEY `Send` (`TrackerID`,`Send`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Data exporting was unselected.
-- Dumping structure for table geoloc.trackers
CREATE TABLE IF NOT EXISTS `trackers` (
  `ID` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `UserID` int(10) unsigned NOT NULL,
  `IMEI` bigint(20) DEFAULT NULL,
  `Name` varchar(45) NOT NULL DEFAULT 'New device',
  `Comment` varchar(135) NOT NULL DEFAULT ' ',
  `IconID` int(10) unsigned NOT NULL DEFAULT '1',
  `HistoryColor` varchar(10) NOT NULL DEFAULT '#ff0000',
  `Period` int(10) unsigned NOT NULL DEFAULT '60',
  `SleepPeriod` int(10) unsigned NOT NULL DEFAULT '600',
  `Phone` varchar(45) NOT NULL DEFAULT ' ',
  `ParkRadius` int(10) unsigned NOT NULL DEFAULT '50',
  `MinParkTime` int(10) unsigned NOT NULL DEFAULT '300',
  `DaysToStore` int(10) unsigned NOT NULL DEFAULT '30',
  `Enable` tinyint(1) NOT NULL DEFAULT '1',
  `FuelExpense` float unsigned NOT NULL DEFAULT '0',
  `Number` int(10) unsigned NOT NULL DEFAULT '10000',
  `MaxSpeed` int(10) unsigned NOT NULL DEFAULT '110',
  `AlarmParkTime` int(10) unsigned NOT NULL DEFAULT '1800',
  `DeviceType` varchar(100) NOT NULL DEFAULT '',
  `FuelExpenseHr` float NOT NULL DEFAULT '0',
  `CreateDate` int(10) unsigned NOT NULL DEFAULT '0',
  `InstallDate` int(10) unsigned NOT NULL DEFAULT '0',
  `StateNumber` varchar(45) NOT NULL DEFAULT '',
  `InstallerName` varchar(128) NOT NULL DEFAULT '',
  `MinIdleTime` int(10) unsigned NOT NULL DEFAULT '180',
  `MinDrain` float NOT NULL DEFAULT '0',
  `Flags` varchar(100) NOT NULL DEFAULT '',
  `MinDrainSpeed` float NOT NULL DEFAULT '0',
  `servergroup` varchar(128) NOT NULL DEFAULT '',
  `DefLat` float NOT NULL DEFAULT '0',
  `DefLng` float NOT NULL DEFAULT '0',
  PRIMARY KEY (`ID`) USING BTREE,
  UNIQUE KEY `IMEI` (`IMEI`) USING BTREE,
  KEY `users` (`UserID`),
  KEY `icons` (`IconID`),
  CONSTRAINT `icons` FOREIGN KEY (`IconID`) REFERENCES `icons` (`ID`) ON UPDATE CASCADE,
  CONSTRAINT `users` FOREIGN KEY (`UserID`) REFERENCES `users` (`ID`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=2479 DEFAULT CHARSET=utf8;

-- Data exporting was unselected.
-- Dumping structure for table geoloc.trackers2groups
CREATE TABLE IF NOT EXISTS `trackers2groups` (
  `GroupID` int(10) unsigned NOT NULL,
  `TrackerID` int(10) unsigned NOT NULL,
  PRIMARY KEY (`GroupID`,`TrackerID`),
  KEY `FK_trackers2groups_2` (`TrackerID`),
  CONSTRAINT `FK_trackers2groups_1` FOREIGN KEY (`GroupID`) REFERENCES `trackergroups` (`ID`) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT `FK_trackers2groups_2` FOREIGN KEY (`TrackerID`) REFERENCES `trackers` (`ID`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Data exporting was unselected.
-- Dumping structure for table geoloc.trackers2users
CREATE TABLE IF NOT EXISTS `trackers2users` (
  `UserID` int(10) unsigned NOT NULL,
  `TrackerID` int(10) unsigned NOT NULL,
  `ACL` int(10) unsigned NOT NULL DEFAULT '0',
  `Name` varchar(45) DEFAULT NULL,
  `Comment` varchar(135) DEFAULT NULL,
  `IconID` int(10) unsigned DEFAULT NULL,
  `HistoryColor` varchar(10) DEFAULT NULL,
  PRIMARY KEY (`UserID`,`TrackerID`),
  KEY `trackers2users_trackers` (`TrackerID`) USING BTREE,
  KEY `trackers2users_icons_idx` (`IconID`) USING BTREE,
  CONSTRAINT `trackers2users_icons` FOREIGN KEY (`IconID`) REFERENCES `icons` (`ID`) ON UPDATE CASCADE,
  CONSTRAINT `trackers2users_trackers` FOREIGN KEY (`TrackerID`) REFERENCES `trackers` (`ID`) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT `trackers2users_users` FOREIGN KEY (`UserID`) REFERENCES `users` (`ID`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Data exporting was unselected.
-- Dumping structure for table geoloc.trackerservice
CREATE TABLE IF NOT EXISTS `trackerservice` (
  `TrackerID` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `InstallDate` int(10) unsigned NOT NULL DEFAULT '0',
  `InstallerName` varchar(255) NOT NULL DEFAULT '',
  `Notes` varchar(16384) NOT NULL DEFAULT '',
  PRIMARY KEY (`TrackerID`),
  CONSTRAINT `FK_trackerservice_1` FOREIGN KEY (`TrackerID`) REFERENCES `trackers` (`ID`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 ROW_FORMAT=COMPACT;

-- Data exporting was unselected.
-- Dumping structure for table geoloc.trends
CREATE TABLE IF NOT EXISTS `trends` (
  `ID` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `TrackerID` int(10) unsigned NOT NULL,
  `ItemID` varchar(20) NOT NULL DEFAULT 'PWR' COMMENT 'ID of device input',
  `Name` varchar(45) NOT NULL DEFAULT 'New',
  `Units` varchar(10) NOT NULL DEFAULT 'мВ',
  `Color` varchar(7) NOT NULL DEFAULT '#00ff00',
  `MinScale` float NOT NULL DEFAULT '0',
  `MaxScale` float NOT NULL DEFAULT '35000',
  `FLAGS` varchar(100) NOT NULL DEFAULT '',
  `Smooth` int(10) unsigned NOT NULL DEFAULT '0',
  `LowAlarm` float DEFAULT NULL,
  `HighAlarm` float DEFAULT NULL,
  `Mask` int(10) unsigned NOT NULL DEFAULT '0',
  PRIMARY KEY (`ID`),
  KEY `FK_trends_1` (`TrackerID`),
  CONSTRAINT `FK_trends_1` FOREIGN KEY (`TrackerID`) REFERENCES `trackers` (`ID`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COMMENT='Trend Desc';

-- Data exporting was unselected.
-- Dumping structure for table geoloc.trendtabtrabsform
CREATE TABLE IF NOT EXISTS `trendtabtrabsform` (
  `TrendID` int(10) unsigned NOT NULL,
  `RawValue` decimal(14,3) NOT NULL DEFAULT '0.000',
  `Value` float NOT NULL DEFAULT '0',
  PRIMARY KEY (`TrendID`,`RawValue`),
  CONSTRAINT `FK_TrendTabTrabsform_1` FOREIGN KEY (`TrendID`) REFERENCES `trends` (`ID`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Data exporting was unselected.
-- Dumping structure for table geoloc.users
CREATE TABLE IF NOT EXISTS `users` (
  `ID` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `Name` varchar(45) NOT NULL,
  `Password` varchar(50) NOT NULL DEFAULT 'E10ADC3949BA59ABBE56E057F20F883E',
  `disabled` int(10) unsigned NOT NULL DEFAULT '0' COMMENT '0 - enable',
  `ACL` bigint(20) unsigned NOT NULL DEFAULT '65535',
  `OrgName` varchar(255) NOT NULL DEFAULT '',
  `ContactName` varchar(255) NOT NULL DEFAULT '',
  `Phone` varchar(255) NOT NULL DEFAULT '',
  `Fax` varchar(255) NOT NULL DEFAULT '',
  `Email` varchar(255) NOT NULL DEFAULT '',
  `PostAdr` varchar(255) NOT NULL DEFAULT '',
  `PasswordEdit` varchar(50) NOT NULL DEFAULT 'E10ADC3949BA59ABBE56E057F20F883E',
  `ParentUserID` int(10) unsigned NOT NULL DEFAULT '0',
  `TimeZone` varchar(100) NOT NULL DEFAULT 'Russian Standard Time',
  `Lang` varchar(45) NOT NULL DEFAULT 'RU',
  `RegionID` int(10) unsigned DEFAULT NULL,
  `Geocoder` varchar(45) NOT NULL DEFAULT '',
  `Flags` varchar(128) NOT NULL DEFAULT '',
  PRIMARY KEY (`ID`),
  KEY `FK_users_1` (`RegionID`),
  CONSTRAINT `FK_users_1` FOREIGN KEY (`RegionID`) REFERENCES `regions` (`ID`) ON DELETE SET NULL
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8;

-- Data exporting was unselected.
-- Dumping structure for view geoloc.vw_forwardrecords
-- Creating temporary table to overcome VIEW dependency errors
CREATE TABLE `vw_forwardrecords` (
	`Tracker-ID` INT(10) UNSIGNED NOT NULL COMMENT 'номер трекера',
	`Device IMEI` BIGINT(20) NULL,
	`Vehicle No` VARCHAR(45) NOT NULL COLLATE 'utf8_general_ci',
	`Location` VARCHAR(128) NOT NULL COLLATE 'utf8_general_ci',
	`Device Time-Stamp (epoch)` INT(11) UNSIGNED NOT NULL COMMENT 'время',
	`Locanix-In Time-Stamp (epoch)` INT(11) NULL,
	`Locanix-Out Time-Stamp (epoch)` INT(11) NULL,
	`Network Delay` BIGINT(12) UNSIGNED NULL,
	`Locanix Delay` BIGINT(12) NULL,
	`Total Delay` BIGINT(12) UNSIGNED NULL,
	`Resend Count` INT(11) NOT NULL COMMENT 'packet No of time Sent',
	`Speed` SMALLINT(5) UNSIGNED NOT NULL COMMENT 'скорость в км/ч',
	`Status` INT(3) NULL COMMENT '1 For Running and 0 For Standing'
) ENGINE=MyISAM;

-- Dumping structure for table geoloc.workcalendar
CREATE TABLE IF NOT EXISTS `workcalendar` (
  `RegionID` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `Year` int(10) unsigned NOT NULL,
  `Calendar` varchar(400) NOT NULL DEFAULT '',
  PRIMARY KEY (`RegionID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 ROW_FORMAT=COMPACT;

-- Data exporting was unselected.
-- Dumping structure for table geoloc.zone2groups
CREATE TABLE IF NOT EXISTS `zone2groups` (
  `GroupID` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `ZoneID` int(10) unsigned NOT NULL,
  PRIMARY KEY (`GroupID`,`ZoneID`),
  KEY `FK_zone2groups_2` (`ZoneID`) USING BTREE,
  CONSTRAINT `FK_zone2groups_1` FOREIGN KEY (`GroupID`) REFERENCES `zonegroups` (`ID`) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT `FK_zone2groups_2` FOREIGN KEY (`ZoneID`) REFERENCES `zones` (`ID`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 ROW_FORMAT=COMPACT;

-- Data exporting was unselected.
-- Dumping structure for table geoloc.zonegroups
CREATE TABLE IF NOT EXISTS `zonegroups` (
  `ID` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `UserID` int(10) unsigned NOT NULL,
  `Name` varchar(45) NOT NULL DEFAULT 'New group',
  PRIMARY KEY (`ID`),
  KEY `FK_zonegroups_1` (`UserID`) USING BTREE,
  CONSTRAINT `FK_zonegroups_1` FOREIGN KEY (`UserID`) REFERENCES `users` (`ID`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 ROW_FORMAT=COMPACT;

-- Data exporting was unselected.
-- Dumping structure for table geoloc.zones
CREATE TABLE IF NOT EXISTS `zones` (
  `ID` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `Name` varchar(45) NOT NULL DEFAULT 'New',
  `Comment` varchar(128) NOT NULL DEFAULT ' ',
  `Radius` int(10) unsigned NOT NULL DEFAULT '100',
  `Points` text NOT NULL,
  `Color` varchar(10) NOT NULL DEFAULT '#FF0000',
  `UserID` int(10) unsigned NOT NULL,
  `Style` varchar(256) NOT NULL DEFAULT '',
  `Flags` varchar(128) NOT NULL DEFAULT '',
  `MaxSpeed` int(10) unsigned NOT NULL DEFAULT '0',
  PRIMARY KEY (`ID`),
  KEY `FK_zones_1` (`UserID`),
  CONSTRAINT `FK_zones_1` FOREIGN KEY (`UserID`) REFERENCES `users` (`ID`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Data exporting was unselected.
-- Dumping structure for table geoloc.zones2users
CREATE TABLE IF NOT EXISTS `zones2users` (
  `ZoneID` int(10) unsigned NOT NULL,
  `UserID` int(10) unsigned NOT NULL,
  `ACL` int(10) unsigned NOT NULL DEFAULT '0',
  PRIMARY KEY (`ZoneID`,`UserID`),
  KEY `FK_zones2users_2` (`UserID`),
  CONSTRAINT `FK_zones2users_1` FOREIGN KEY (`ZoneID`) REFERENCES `zones` (`ID`) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT `FK_zones2users_2` FOREIGN KEY (`UserID`) REFERENCES `users` (`ID`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Data exporting was unselected.
-- Dumping structure for view geoloc.vw_forwardrecords
-- Removing temporary table and create final VIEW structure
DROP TABLE IF EXISTS `vw_forwardrecords`;
CREATE ALGORITHM=UNDEFINED DEFINER=`geoloc`@`%` SQL SECURITY DEFINER VIEW `vw_forwardrecords` AS select `points`.`TrackerID` AS `Tracker-ID`,`trackers`.`IMEI` AS `Device IMEI`,`trackers`.`Name` AS `Vehicle No`,`trackers`.`servergroup` AS `Location`,`points`.`Time` AS `Device Time-Stamp (epoch)`,`points`.`ReceivedTime` AS `Locanix-In Time-Stamp (epoch)`,`points`.`ForwardTime` AS `Locanix-Out Time-Stamp (epoch)`,(`points`.`ReceivedTime` - `points`.`Time`) AS `Network Delay`,(`points`.`ForwardTime` - `points`.`ReceivedTime`) AS `Locanix Delay`,(`points`.`ForwardTime` - `points`.`Time`) AS `Total Delay`,`points`.`NTS` AS `Resend Count`,`points`.`speed` AS `Speed`,`points`.`VehicleStatus` AS `Status` from (`points` join `trackers` on((`trackers`.`ID` = `points`.`TrackerID`))) where ((`points`.`ForwardTime` > 0) and (`points`.`ReceivedTime` > 0) and (`points`.`ReceivedTime` > `points`.`Time`));

/*!40101 SET SQL_MODE=IFNULL(@OLD_SQL_MODE, '') */;
/*!40014 SET FOREIGN_KEY_CHECKS=IF(@OLD_FOREIGN_KEY_CHECKS IS NULL, 1, @OLD_FOREIGN_KEY_CHECKS) */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
