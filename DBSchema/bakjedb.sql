CREATE DATABASE  IF NOT EXISTS `bakjedb` /*!40100 DEFAULT CHARACTER SET utf8 */;
USE `bakjedb`;
-- MySQL dump 10.13  Distrib 5.7.17, for Win64 (x86_64)
--
-- Host: 127.0.0.1    Database: bakjedb
-- ------------------------------------------------------
-- Server version	5.7.18-log

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `authkeys`
--

DROP TABLE IF EXISTS `authkeys`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `authkeys` (
  `iduser` varchar(20) NOT NULL,
  `authkey` varchar(45) NOT NULL,
  PRIMARY KEY (`iduser`),
  UNIQUE KEY `authkey_UNIQUE` (`authkey`),
  CONSTRAINT `iduser` FOREIGN KEY (`iduser`) REFERENCES `user` (`iduser`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `notice`
--

DROP TABLE IF EXISTS `notice`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `notice` (
  `idnotice` int(11) NOT NULL AUTO_INCREMENT,
  `title` varchar(128) DEFAULT NULL,
  `description` text,
  `datetime` datetime DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`idnotice`)
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `postings`
--

DROP TABLE IF EXISTS `postings`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `postings` (
  `idposting` int(11) NOT NULL AUTO_INCREMENT,
  `authorid` varchar(20) NOT NULL,
  `title` varchar(256) NOT NULL,
  `description` text,
  `sourceurl` varchar(256) DEFAULT NULL,
  `datetime` datetime DEFAULT CURRENT_TIMESTAMP,
  `is_private` tinyint(1) DEFAULT '0',
  `is_blinded` tinyint(1) DEFAULT '0',
  PRIMARY KEY (`idposting`),
  KEY `authorid_idx` (`authorid`),
  CONSTRAINT `authorid` FOREIGN KEY (`authorid`) REFERENCES `user` (`iduser`) ON DELETE NO ACTION ON UPDATE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=13 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `report`
--

DROP TABLE IF EXISTS `report`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `report` (
  `idreport` int(11) NOT NULL AUTO_INCREMENT,
  `reporterid` varchar(20) NOT NULL,
  `report_type` enum('posting','user','bug') NOT NULL,
  `shortdesc` varchar(128) NOT NULL,
  `longdesc` text,
  PRIMARY KEY (`idreport`)
) ENGINE=InnoDB AUTO_INCREMENT=7 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `report_reason_posting`
--

DROP TABLE IF EXISTS `report_reason_posting`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `report_reason_posting` (
  `reportid` int(11) NOT NULL,
  `reasoncode` int(11) NOT NULL,
  `postingid` int(11) NOT NULL,
  PRIMARY KEY (`reportid`),
  KEY `fk_reported_posting_idx` (`postingid`),
  CONSTRAINT `fk_posting_report_id` FOREIGN KEY (`reportid`) REFERENCES `report` (`idreport`) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT `fk_reported_posting` FOREIGN KEY (`postingid`) REFERENCES `postings` (`idposting`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `report_reason_user`
--

DROP TABLE IF EXISTS `report_reason_user`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `report_reason_user` (
  `reportid` int(11) NOT NULL,
  `reasoncode` int(11) NOT NULL,
  `userid` varchar(20) NOT NULL,
  PRIMARY KEY (`reportid`),
  KEY `fk_reported_user_idx` (`userid`),
  CONSTRAINT `fk_reported_user` FOREIGN KEY (`userid`) REFERENCES `user` (`iduser`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `fk_user_report_id` FOREIGN KEY (`reportid`) REFERENCES `report` (`idreport`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `screenshots`
--

DROP TABLE IF EXISTS `screenshots`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `screenshots` (
  `idscreenshot` int(11) NOT NULL AUTO_INCREMENT,
  `postingid` int(11) NOT NULL,
  `imagedata` blob NOT NULL,
  PRIMARY KEY (`idscreenshot`),
  KEY `posting_idx` (`postingid`),
  CONSTRAINT `screenshot_to_posting` FOREIGN KEY (`postingid`) REFERENCES `postings` (`idposting`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `tags`
--

DROP TABLE IF EXISTS `tags`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `tags` (
  `idtag` int(11) NOT NULL AUTO_INCREMENT,
  `name` varchar(45) NOT NULL,
  `postingid` int(11) NOT NULL,
  `taguserid` varchar(20) NOT NULL,
  PRIMARY KEY (`idtag`),
  KEY `posting_idx` (`postingid`),
  KEY `tagger_idx` (`taguserid`),
  CONSTRAINT `posting` FOREIGN KEY (`postingid`) REFERENCES `postings` (`idposting`) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT `tagger` FOREIGN KEY (`taguserid`) REFERENCES `user` (`iduser`) ON DELETE NO ACTION ON UPDATE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `user`
--

DROP TABLE IF EXISTS `user`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `user` (
  `iduser` varchar(20) NOT NULL,
  `password` varchar(20) NOT NULL,
  `email` varchar(45) DEFAULT NULL,
  `is_admin` tinyint(1) NOT NULL DEFAULT '0',
  `is_blinded` tinyint(1) NOT NULL DEFAULT '0',
  PRIMARY KEY (`iduser`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping events for database 'bakjedb'
--

--
-- Dumping routines for database 'bakjedb'
--
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2017-06-01 13:36:35
