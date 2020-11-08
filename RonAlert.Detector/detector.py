
# install dependencies: 
import torch, torchvision
print(torch.__version__, torch.cuda.is_available())
# See https://detectron2.readthedocs.io/tutorials/install.html for instructions
assert torch.__version__.startswith("1.7")


# Some basic setup:
# Setup detectron2 logger
import detectron2
from detectron2.utils.logger import setup_logger
setup_logger()

# import some common libraries
import time
import torch
import numpy as np
import cv2
import cv2.aruco as aruco
import math
import tqdm
import os, json, cv2, random
import requests
from scipy.spatial.distance import pdist, squareform
from google.colab.patches import cv2_imshow
from google.colab import files
import facemask

# import some common detectron2 utilities
from detectron2 import model_zoo
from detectron2.engine import DefaultPredictor
from detectron2.config import get_cfg
from detectron2.utils.visualizer import Visualizer, ColorMode
from detectron2.data import MetadataCatalog, DatasetCatalog

import atexit
import bisect
import multiprocessing as mp
from collections import deque

from detectron2.data import MetadataCatalog
from detectron2.engine.defaults import DefaultPredictor
from detectron2.utils.video_visualizer import VideoVisualizer


cfg = get_cfg()
# add project-specific config (e.g., TensorMask) here if you're not running a model in detectron2's core library
cfg.merge_from_file(model_zoo.get_config_file("COCO-InstanceSegmentation/mask_rcnn_R_50_FPN_3x.yaml"))
cfg.MODEL.ROI_HEADS.SCORE_THRESH_TEST = 0.5  # set threshold for this model
# Find a model from detectron2's model zoo. You can use the https://dl.fbaipublicfiles... url as well
cfg.MODEL.WEIGHTS = model_zoo.get_checkpoint_url("COCO-InstanceSegmentation/mask_rcnn_R_50_FPN_3x.yaml")
predictor = DefaultPredictor(cfg)
outputs = predictor(im)

# look at the outputs. See https://detectron2.readthedocs.io/tutorials/models.html#model-output-format for specification
print(outputs["instances"].pred_classes)
print(outputs["instances"].pred_boxes)
print(outputs["instances"].pred_masks)

# We can use `Visualizer` to draw the predictions on the image.
v = Visualizer(im[:, :, ::-1], MetadataCatalog.get(cfg.DATASETS.TRAIN[0]), scale=1.2)
out = v.draw_instance_predictions(outputs["instances"].to("cpu"))
cv2_imshow(out.get_image()[:, :, ::-1])

class VisualizationDemo(object):
    def __init__(self, cfg, instance_mode=ColorMode.IMAGE, parallel=False):
        """
        Args:
            cfg (CfgNode):
            instance_mode (ColorMode):
            parallel (bool): whether to run the model in different processes from visualization.
                Useful since the visualization logic can be slow.
        """
        self.metadata = MetadataCatalog.get(
            cfg.DATASETS.TEST[0] if len(cfg.DATASETS.TEST) else "__unused"
        )
        self.cpu_device = torch.device("cpu")
        self.instance_mode = instance_mode

        self.parallel = parallel
        if parallel:
            num_gpu = torch.cuda.device_count()
            self.predictor = AsyncPredictor(cfg, num_gpus=num_gpu)
        else:
            self.predictor = DefaultPredictor(cfg)

    def run_on_image(self, image):
        """
        Args:
            image (np.ndarray): an image of shape (H, W, C) (in BGR order).
                This is the format used by OpenCV.

        Returns:
            predictions (dict): the output of the model.
            vis_output (VisImage): the visualized image output.
        """
        vis_output = None
        predictions = self.predictor(image)
        # Convert image from OpenCV BGR format to Matplotlib RGB format.
        image = image[:, :, ::-1]
        visualizer = Visualizer(image, self.metadata, instance_mode=self.instance_mode)
        if "panoptic_seg" in predictions:
            panoptic_seg, segments_info = predictions["panoptic_seg"]
            vis_output = visualizer.draw_panoptic_seg_predictions(
                panoptic_seg.to(self.cpu_device), segments_info
            )
        else:
            if "sem_seg" in predictions:
                vis_output = visualizer.draw_sem_seg(
                    predictions["sem_seg"].argmax(dim=0).to(self.cpu_device)
                )
            if "instances" in predictions:
                instances = predictions["instances"].to(self.cpu_device)
                vis_output = visualizer.draw_instance_predictions(predictions=instances)

        return predictions, vis_output

    def _frame_from_video(self, video):
        idx = 0
        while video.isOpened():
            success, frame = video.read()
            if success:
                if idx % self.nth_frame == 0:
                    yield self.rescale_frame(frame)
                idx += 1
            else:
                break

    def rescale_frame(self, frame):
        width = min(frame.shape[1], self.max_w)
        ratio = frame.shape[1] / frame.shape[0]
        height = int(width / ratio)
        dim = (width, height)
        return cv2.resize(frame, dim, interpolation=cv2.INTER_AREA)

    def run_on_video(self, video, max_w, nth_frame, draw):
        """
        Visualizes predictions on frames of the input video.

        Args:
            video (cv2.VideoCapture): a :class:`VideoCapture` object, whose source can be
                either a webcam or a video file.

        Yields:
            ndarray: BGR visualizations of each video frame.
        """
        self.max_w = max_w
        self.nth_frame = nth_frame
        video_visualizer = VideoVisualizer(self.metadata, self.instance_mode)

        def process_predictions(frame, predictions):
            predictions = {"instances": filter_persons(predictions)}
            fc = augment(frame)

            if not draw:
                return frame, None, predictions, fc

            frame_bgr = cv2.cvtColor(frame, cv2.COLOR_RGB2BGR)
            if "panoptic_seg" in predictions:
                panoptic_seg, segments_info = predictions["panoptic_seg"]
                vis_frame = video_visualizer.draw_panoptic_seg_predictions(
                    frame_bgr, panoptic_seg.to(self.cpu_device), segments_info
                )
            elif "instances" in predictions:
                predictions = predictions["instances"].to(self.cpu_device)
                vis_frame = video_visualizer.draw_instance_predictions(frame_bgr, predictions)
            elif "sem_seg" in predictions:
                vis_frame = video_visualizer.draw_sem_seg(
                    frame_bgr, predictions["sem_seg"].argmax(dim=0).to(self.cpu_device)
                )

            # Converts Matplotlib RGB format to OpenCV BGR format
            vis_frame = cv2.cvtColor(vis_frame.get_image(), cv2.COLOR_RGB2BGR)
            return frame, vis_frame, predictions, fc

        frame_gen = self._frame_from_video(video)
        if self.parallel:
            buffer_size = self.predictor.default_buffer_size

            frame_data = deque()

            for cnt, frame in enumerate(frame_gen):
                frame_data.append(frame)
                self.predictor.put(frame)

                if cnt >= buffer_size:
                    frame = frame_data.popleft()
                    predictions = self.predictor.get()
                    yield process_predictions(frame, predictions)

            while len(frame_data):
                frame = frame_data.popleft()
                predictions = self.predictor.get()
                yield process_predictions(frame, predictions)
        else:
            for frame in frame_gen:
                yield process_predictions(frame, self.predictor(frame))

clss = MetadataCatalog.get(cfg.DATASETS.TRAIN[0]).thing_classes
pred_idxs = outputs["instances"].pred_classes.cpu()
# print([clss[pred_idx] for pred_idx in pred_idxs])

def filter_persons(outputs):
  pred_idxs = outputs["instances"].pred_classes.cpu()
  is_person = np.array(pred_idxs) == 0
  return outputs["instances"][is_person]

persons = filter_persons(outputs)

bbox = persons[0].pred_boxes[0][0]
# print(bbox)
mask = persons[0].pred_masks[0].cpu().numpy()
# print(persons.pred_masks[0].cpu().numpy())

def get_center(person):
  arr = person.pred_masks[0].cpu().numpy()
  return np.median(np.nonzero(arr), axis=1)

def get_centers(outputs):
  persons = outputs
  return [get_center(persons[i]) for i in range(len(persons))]

# get_centers(outputs)

guid = "9e9c13b6-3fc6-4875-bd37-b9511de0b082"
hostname = "https://ronalert-api.azurewebsites.net"
path = f"/api/room/{guid}/people"
url = f"{hostname}{path}"

def send_data(api_data):
  return requests.post(url, json=api_data)

# send_data({"faceMask": True, "positionX": 0.,"positionY": 0., "nearestDistance: 0.0"})

def augment(frame):
    camera_angle = 90
    height, width, channels = frame.shape
    gray = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)
    aruco_dict = aruco.Dictionary_get(aruco.DICT_4X4_1000)
    arucoParameters = aruco.DetectorParameters_create()
    corners, ids, rejectedImgPoints = aruco.detectMarkers(
        gray, aruco_dict, parameters=arucoParameters)
    objects = []
    for corner in corners:
        #corner = corner[0]
        diagonal1 = math.sqrt( ((corner[0][0][0]-corner[0][2][0])**2)+((corner[0][0][1]-corner[0][2][1])**2))
        diagonal2 = math.sqrt( ((corner[0][1][0]-corner[0][3][0])**2)+((corner[0][1][1]-corner[0][3][1])**2))
        diagonal = (diagonal1 + diagonal2) / 2
        x_mean = (corner[0][0][0] + corner[0][1][0] + corner[0][2][0] + corner[0][3][0])/4
        l_l = (width * math.sqrt(800)) / diagonal
        y = (l_l)/(2*math.tan(camera_angle/2))
        x = (x_mean / width) * l_l
        objects.append([x,y])
    return objects

def find_markers(frame):
  gray = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)
  aruco_dict = aruco.Dictionary_get(aruco.DICT_4X4_1000)
  arucoParameters = aruco.DetectorParameters_create()
  corners, ids, rejectedImgPoints = aruco.detectMarkers(gray, aruco_dict, parameters=arucoParameters)
  return corners

model = facemask.FaceMaskDetector("model_facemask.pth")

def detect_face_masks(frame, predictions, vis_frame):
  masks = []
  for i in range(len(predictions)):
    bbox = np.round(np.array(predictions[i].pred_boxes[0].tensor)).astype(np.int32)[0]
    cut = frame[bbox[1]:bbox[3], bbox[0]:bbox[2]]
    face_mask_data = model(cut)

    masks.append(len(face_mask_data) > 0 and face_mask_data[0][2] == 'face_mask')
    if len(face_mask_data) > 0:
      color = (255, 0, 0)
      if face_mask_data[0][2] == 'face_mask':
        color = (0, 0, 255)
      fm_bbox = np.round(face_mask_data[0][0]).astype(np.int32)
      
      vis_frame = cv2.rectangle(vis_frame, 
                                (bbox[0] + fm_bbox[0], bbox[1] + fm_bbox[1]),
                                (bbox[0] + fm_bbox[2], bbox[1] + fm_bbox[3]), color, 1)

  return masks, vis_frame

video_input = "INPUT.mp4"
video_output = "OUTPUT.mp4"

max_w = 800
nth_frame = 5
draw = False
send = True

demo = VisualizationDemo(cfg)

video = cv2.VideoCapture(video_input)
width = int(video.get(cv2.CAP_PROP_FRAME_WIDTH))
height = int(video.get(cv2.CAP_PROP_FRAME_HEIGHT))
frames_per_second = video.get(cv2.CAP_PROP_FPS)
num_frames = int(video.get(cv2.CAP_PROP_FRAME_COUNT))

new_width = min(width, max_w)
ratio = width / height
new_height = int(new_width / ratio)
print((width, height), (new_width, new_height))

if video_output != "":
  if os.path.isfile(video_output):
    os.remove(video_output)
  output_file = cv2.VideoWriter(
    filename=video_output,
    # some installation of opencv may not support x264 (due to its license),
    # you can try other format (e.g. MPEG)
    fourcc=cv2.VideoWriter_fourcc(*"x264"),
    fps=float(frames_per_second // nth_frame),
    frameSize=(new_width, new_height),
    isColor=True)
  
assert os.path.isfile(video_input)
video_stream = demo.run_on_video(video, max_w, nth_frame, draw or video_output != "")

for fvp in tqdm.tqdm(video_stream, total=num_frames // nth_frame):
  frame, vis_frame, predictions, fixed_centers = fvp
  
  # centers = get_centers(predictions)
  # print(centers, fixed_centers)

  if len(predictions) != len(fixed_centers):
    pass
    # continue

  corners = find_markers(frame)
  vis_frame = aruco.drawDetectedMarkers(vis_frame, corners)

  face_masks, vis_frame = detect_face_masks(frame, predictions, vis_frame)
  dists = squareform(pdist(fixed_centers))
  dists[dists == 0] = 99999
  min_dists = np.min(dists, axis=0)

  if send:
    api_data = []
    for i in range(len(fixed_centers)):
      api_data.append({
          "face_mask": face_masks[i],
          "positionX": fixed_centers[i][0],
          "positionY": fixed_centers[i][1],
          "nearestDistance": min_dists[i]
      })
    send_data(api_data)

  if video_output != "":
    output_file.write(vis_frame)
  if draw:
    cv2_imshow(vis_frame)

video.release()
if video_output != "":
  output_file.release()
if draw:
  cv2.destroyAllWindows()
