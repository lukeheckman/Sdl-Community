﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Sdl.Community.StarTransit.Shared.Models;
using Sdl.Desktop.IntegrationApi;
using Sdl.ProjectApi.Settings;
using Sdl.ProjectAutomation.Core;
using Sdl.ProjectAutomation.FileBased;
using Sdl.TranslationStudioAutomation.IntegrationApi;

namespace Sdl.Community.StarTransit.Shared.Services
{
    public class ReturnPackageService: AbstractViewControllerAction<ProjectsController> 

    {
        public ReturnPackageService()
        {

        }

        /// <summary>
        /// Returns a list of StarTransit return package and  true if the projects selected are a StarTransit projects 
        /// </summary>
        /// <returns></returns>
        public Tuple<List<ReturnPackage>, bool> GetReturnPackage()
        {
            var projects = Controller.SelectedProjects.ToList();
            var returnPackageList= new List<ReturnPackage>();
            List<bool> isTransitProject = new List<bool>();

            foreach (var project in projects)
            {
              
                var targetFiles = project.GetTargetLanguageFiles().ToList();
                var isTransit=IsTransitProject(targetFiles);
                if (isTransit)
                {
                    var returnPackage = new ReturnPackage
                    {
                        FileBasedProject = project,
                        ProjectLocation = project.FilePath,
                        TargetFiles = targetFiles
                    };
                   returnPackageList.Add(returnPackage);      
                    isTransitProject.Add(true);
                }
                else
                {
                    isTransitProject.Add(false);
                }

            }
            
            if (isTransitProject.Contains(false))
            {
                return new Tuple<List<ReturnPackage>, bool>(returnPackageList, false);
            }
            return new Tuple<List<ReturnPackage>, bool>(returnPackageList, true);
        }

        /// <summary>
        /// Check to see if the file type is the same with the Transit File Type
        /// </summary>
        /// <param name="filesPath"></param>
        /// <returns></returns>
        public bool IsTransitProject(List<ProjectFile> filesPath)
        {
            var areTranstFiles = new List<bool>();
           foreach (var file in filesPath)
            {
                if (file.FileTypeId.Equals("Transit File Type 1.0.0.0"))
                {
                    areTranstFiles.Add(true);
                }
                else
                {
                    areTranstFiles.Add(false);
                    return  false;
                }
            }

            return true;
        }

        
        protected override void Execute()
        {
            
        }

        /// <summary>
        /// TO DO :The files are exported in studio project, they should be imported in a custom location
        /// </summary>
        /// <param name="package"></param>
        public void ExportFiles(ReturnPackage package)
        {
            var outputFilesPathList = new List<TaskFileInfo>();
            

                package.FileBasedProject.RunAutomaticTask(package.TargetFiles.GetIds(), AutomaticTaskTemplateIds.Scan);
             
                var taskSequence = package.FileBasedProject.RunAutomaticTasks(package.TargetFiles.GetIds(), new string[]
                {
                    AutomaticTaskTemplateIds.ExportFiles


                });

                var outputFiles = taskSequence.OutputFiles.ToList();
                outputFilesPathList.AddRange(outputFiles);

          //  CreateArchive(package.Location, outputFilesPathList);
          
        }


        /// <summary>
        /// Creates an archive in the Return Package folder and add project files to it
        /// For the moment we add the files without runing any task on them
        /// </summary>
        /// <param name="packagePath"></param>
        /// <param name="projectFiles"></param>
        private void CreateArchive(string packagePath, List<TaskFileInfo> projectFiles)
        {
            var archivePath = Path.Combine(packagePath, "returnPackage.tpf");
            using (var archive = ZipFile.Open(archivePath, ZipArchiveMode.Create))
            {
                foreach (var file in projectFiles)
                {
                    //parameters :file path, file name
                    //we can use CreateFromFolder method once we manage to save the target files in a custom folder after we run the task
                   
                    //archive.CreateEntryFromFile(file., file.Name, CompressionLevel.Optimal);
                }

            }
        }
    }
}
