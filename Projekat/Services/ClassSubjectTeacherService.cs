﻿using AutoMapper;
using NLog;
using Projekat.Models;
using Projekat.Models.DTOs;
using Projekat.Repository;
using Projekat.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Projekat.Services
{
    public class ClassSubjectTeacherService : IClassSubjectTeacherService
    {
        private IUnitOfWork db;
        private ITeacherService teacherService;
        private ISubjectService subjectService;
        private ISubjectTeacherService stService;
        
        private IClassService classService;
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public ClassSubjectTeacherService(IUnitOfWork db, ITeacherService teacherService, ISubjectService subjectService, 
            ISubjectTeacherService stService, IClassService classService)
        {
            this.db = db;
            this.teacherService = teacherService;
            this.subjectService = subjectService;
            this.stService = stService;
            this.classService = classService;
        }

        public ClassSubjectTeacherDTO Create(string teacherId, int subjectId, int classId)
        {

            Teacher teacher = teacherService.GetById(teacherId);
            Subject subject = subjectService.GetById(subjectId);
            Class clas = classService.GetById(classId);

            if (clas == null)
            {
                logger.Info("Exception - Class with id " + classId + " doesn't exist.");
                throw new Exception("Class with id " + classId + " doesn't exist.");
            }
            SubjectTeacher subjectTeacher = stService.GetBySubjectAndTeacher(subjectId, teacherId);
            if (subjectTeacher == null)
            {
                logger.Info("Exception - Teacher with id " + teacherId + " doesn't teach a subject with id " + subjectId);
                throw new Exception("Teacher with id " + teacherId + " doesn't teach a subject with id " + subjectId);
            }
            //ako ne postoji ta kombinacija u tabeli
            if (db.ClassSubjectTeachersRepository.GetByClassSubjectTeacher(classId, subjectTeacher.ID) == null)
            {
                if (subject.Year == clas.Year)
                {
                    ClassSubjectTeacher found = db.ClassSubjectTeachersRepository.Get(x => x.SubjectTeacher.Subject.ID == subjectId && x.Class.ID == classId).FirstOrDefault();
                    if (found == null)
                    {
                        ClassSubjectTeacher created = new ClassSubjectTeacher();
                        created.SubjectTeacher = subjectTeacher;
                        created.Class = clas;

                        subjectTeacher.TaughtSubjectClasses.Add(created);
                        clas.AttendedTeacherSubjects.Add(created);

                        db.ClassSubjectTeachersRepository.Insert(created);
                        db.Save();

                        return Mapper.Map<ClassSubjectTeacherDTO>(created);
                    }
                    
                    logger.Info("Exception - Another teacher already teaches that subject in that class.");
                    throw new Exception("Another teacher already teaches that subject in that class.");

                }
                logger.Info("Exception - teacher doesn't teach that subject in that year.");
                throw new Exception("Teacher doesn't teach that subject in that year.");
            }
            logger.Info("Exception - teacher already teaches that subject in this class.");
            throw new Exception("Teacher already teaches that subject in this class.");
            
        }


       

        public ClassSubjectTeacherDTO Update(int id, string teacherId, int subjectId, int classId)
        {

            ClassSubjectTeacher found = db.ClassSubjectTeachersRepository.GetByID(id);
            if(found == null)
            {
                throw new Exception("Class-subject-teacher with id " + id + " not found.");
            }

            Teacher teacher = teacherService.GetById(teacherId);
            Subject subject = subjectService.GetById(subjectId);
            Class clas = classService.GetById(classId);
            if (clas == null)
            {
                throw new Exception("Class with that id doesn't exist.");
            }
            SubjectTeacher subjectTeacher = stService.GetBySubjectAndTeacher(subjectId, teacherId);
            if(subjectTeacher == null)
            {
                throw new Exception("Teacher doesn't teach that subject.");
            }
            if (subject.Year == clas.Year)
            {
                //ako ne postoji ta kombinacija u tabeli
                if (db.ClassSubjectTeachersRepository.Get(x => x.SubjectTeacher.ID == subjectTeacher.ID && x.Class.ID == classId).FirstOrDefault() == null)
                {
                   // ako tom odeljenju neko predaje dozvoli izmenu
                    ClassSubjectTeacher cst = db.ClassSubjectTeachersRepository.Get(x => x.SubjectTeacher.Subject.ID == subjectId && x.Class.ID == classId).FirstOrDefault();
                    if (cst != null)
                    {
                        clas.AttendedTeacherSubjects.Remove(cst);
                        cst.SubjectTeacher.TaughtSubjectClasses.Remove(cst);

                       
                    }

                    found.SubjectTeacher = subjectTeacher;
                    found.SubjectTeacher.TaughtSubjectClasses.Remove(found);
                    found.Class = clas;

                    subjectTeacher.TaughtSubjectClasses.Add(found);
                    clas.AttendedTeacherSubjects.Add(found);

                    db.ClassSubjectTeachersRepository.Update(found);
                    db.Save();

                    return Mapper.Map<ClassSubjectTeacherDTO>(found);
                   
                }
                throw new Exception("Teacher already teaches that subject in this class.");
            }
            throw new Exception("Subject is not taught in the year which that class attends.");

        }

        public ClassSubjectTeacherDTO Delete(int id)
        {
            ClassSubjectTeacher found = db.ClassSubjectTeachersRepository.GetByID(id);

            if (found != null)
            {
                if(found.Grades.Count() ==0)
                {

                    SubjectTeacher subjectTeacher = found.SubjectTeacher;
                    Class clas = found.Class;

                    subjectTeacher.TaughtSubjectClasses.Remove(found);
                    clas.AttendedTeacherSubjects.Remove(found);

                    db.ClassSubjectTeachersRepository.Delete(id);
                    db.Save();
                    return Mapper.Map<ClassSubjectTeacher, ClassSubjectTeacherDTO>(found);
                }
                throw new Exception("Can't delete because students have grades.");
            }
            return null;

        }

        public IEnumerable<ClassSubjectTeacher> GetAll()
        {
            return db.ClassSubjectTeachersRepository.Get();
        }

        public IEnumerable<ClassSubjectTeacherDTO> GetAllDTOs()
        {
            return db.ClassSubjectTeachersRepository.Get().ToList().Select(Mapper.Map<ClassSubjectTeacher, ClassSubjectTeacherDTO>);
        }

        public ClassSubjectTeacher GetById(int Id)
        {
            return db.ClassSubjectTeachersRepository.GetByID(Id);
        }

        public ClassSubjectTeacherDTO GetDtoById(int Id)
        {
            return Mapper.Map<ClassSubjectTeacherDTO>(GetById(Id));
        }

        public ClassSubjectTeacherDTO RemoveClassFromSubjectTeacher(string id, int isSubject)
        {
            throw new NotImplementedException();
        }

        public ClassSubjectTeacherDTO Remove(int subjectId, string teacherId, int classId)
        {
            throw new NotImplementedException();
        }

        public ClassSubjectTeacher GetByClassSubjectTeacher(int classId, int subjectTeacherId)
        {
            ClassSubjectTeacher cst = db.ClassSubjectTeachersRepository.GetByClassSubjectTeacher(classId, subjectTeacherId);
            if(cst == null)
            {
                logger.Info("Exception - teacher doesn't teach that subject in class which student attends");
                throw new Exception("Teacher doesn't teach required subject in that class!");
            }
            return cst;
        }

        public IEnumerable<ClassSubjectTeacher> GetByTeacher(string id)
        {

            IEnumerable<ClassSubjectTeacher> csts = db.ClassSubjectTeachersRepository.GetByTeacher(id);
            if (csts.Count() == 0)
            {
                logger.Info("Exception - teacher doesn't teach any subject in class.");
                throw new Exception("Teacher doesn't teach subjects in any class and is not authorized to see grades!");
            }
            return csts;
        }

        public IEnumerable<ClassSubjectTeacher> GetByClass(int classId)
        {
            IEnumerable<ClassSubjectTeacher> csts = db.ClassSubjectTeachersRepository.GetByClass(classId);
            if (csts.Count() == 0)
            {
                logger.Info("Exception - class doesn't attends any subjects.");
                throw new Exception(" Class doesn't attends any subjects.");
            }
            return csts;
        }

        public IEnumerable<ClassSubjectTeacher> GetByClassTeacher(int classId, string teacherId)
        {
            IEnumerable<ClassSubjectTeacher> csts = db.ClassSubjectTeachersRepository.GetByClassTeacher(classId, teacherId);
            if (csts.Count() == 0)
            {
                logger.Info("Exception - teacher doesn't teach any subjects in this class.");
                throw new Exception("Teacher doesn't teach any subjects in this class.");
            }
            return csts;
        }

        public ClassSubjectTeacherDTO GetByCST(int classId, int subjectId, string teacherId)
        {
            return Mapper.Map<ClassSubjectTeacherDTO>(db.ClassSubjectTeachersRepository.GetByCST(classId, subjectId, teacherId));

        }

        public ClassSubjectTeacherDTO Create1(int subTeacher, int classId)
        {
            SubjectTeacher st = stService.GetById(subTeacher);
            Class clas = classService.GetById(classId);

            if (clas == null)
            {
                logger.Info("Exception - Class with id " + classId + " doesn't exist.");
                throw new Exception("Class with id " + classId + " doesn't exist.");
            }
            
            if (st == null)
            {
                logger.Info("Subject-teacher with id " + subTeacher + " doesn't exist");
                throw new Exception("Subject-teacher doesn't exist");
            }
            if (db.ClassSubjectTeachersRepository.GetByClassSubjectTeacher(classId, subTeacher) !=null)
            {
                throw new Exception("Teacher already teaches that subject in that class.");
            }
            if (st.Subject.Year != clas.Year)
            {
                throw new Exception("Can not add a subject taught in other year!");
            }
            ClassSubjectTeacher cst = new ClassSubjectTeacher();
            cst.Class = clas;
            cst.SubjectTeacher = st;

            clas.AttendedTeacherSubjects.Add(cst);
            st.TaughtSubjectClasses.Add(cst);

            db.ClassSubjectTeachersRepository.Insert(cst);
            db.Save();

            return Mapper.Map<ClassSubjectTeacherDTO>(cst);
        }

        public ClassSubjectTeacherDTO RemoveSubjectFromClass(int classId, int stId)
        {
            ClassSubjectTeacher cst = GetByClassSubjectTeacher(classId, stId);
            if (cst == null)
            {
                logger.Info("Subject isn't taught by that teacher in thant class.");
                throw new Exception("Subject isn't taught by that teacher in thant class.");
            }

            IEnumerable<Grade> grades = db.GradesRepository.GetByClassSubjectTeacher(cst.ID);
            if (grades.Count() > 0)
            {
                logger.Info("Exception - Can't delete subject that has grades.");
                throw new Exception("Can't delete subject that has grades.");
            }
            Class clas = cst.Class;
            SubjectTeacher st = cst.SubjectTeacher;


            clas.AttendedTeacherSubjects.Remove(cst);
            st.TaughtSubjectClasses.Remove(cst);
            db.ClassSubjectTeachersRepository.Delete(cst);
            db.Save();
            return Mapper.Map<ClassSubjectTeacher, ClassSubjectTeacherDTO>(cst);




        }
    }
    
}